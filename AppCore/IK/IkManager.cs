using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;
using MiniMikuDance.Util;

namespace MiniMikuDance.IK;

public static class IkManager
{
    private static readonly Dictionary<int, IkBone> BonesDict = new();
    private static IReadOnlyList<BoneData>? _modelBones;

    // レンダラーから提供される各種処理を委譲用デリゲートとして保持
    public static System.Func<float, float, int>? PickFunc { get; set; }
    public static System.Func<int, Vector3>? GetBonePositionFunc { get; set; }
    public static System.Func<Vector3>? GetCameraPositionFunc { get; set; }
    public static System.Action<int, OpenTK.Mathematics.Vector3>? SetBoneRotation { get; set; }
    public static System.Func<Vector3, Vector3>? ToModelSpaceFunc { get; set; }
    public static System.Action? InvalidateViewer { get; set; }

    private static int _selectedBoneIndex = -1;
    private static Plane _dragPlane;

    public static int SelectedBoneIndex => _selectedBoneIndex;
    public static Plane DragPlane => _dragPlane;

    public static IReadOnlyDictionary<int, IkBone> Bones => BonesDict;

    public static void LoadPmxIkBones(IReadOnlyList<BoneData> modelBones)
    {
        Clear();
        _modelBones = modelBones;
        for (int i = 0; i < modelBones.Count; i++)
        {
            var ik = modelBones[i].Ik;
            if (ik == null)
                continue;

            RegisterIkBone(i, modelBones[i]);
            // IK ターゲットおよびリンクボーンも登録
            RegisterChainBones(ik);
        }

        // "足IK" ボーンが取りこぼされていないか確認する
        for (int i = 0; i < modelBones.Count; i++)
        {
            if (BonesDict.ContainsKey(i))
                continue;
            var name = modelBones[i].Name;
            if (name.Contains("足", StringComparison.Ordinal) &&
                (name.Contains("IK", StringComparison.OrdinalIgnoreCase) || name.Contains("ＩＫ")))
            {
                RegisterIkBone(i, modelBones[i]);
            }
        }
    }

    private static void RegisterIkBone(int index, BoneData bRoot, bool isEffector = false)
    {
        var rootPos = Vector3.Transform(Vector3.Zero, bRoot.BindMatrix);
        var tipPos = rootPos;
        float maxLen = 0f;
        if (_modelBones != null)
        {
            for (int i = 0; i < _modelBones.Count; i++)
            {
                var child = _modelBones[i];
                if (child.Parent != index)
                    continue;
                var childPos = Vector3.Transform(Vector3.Zero, child.BindMatrix);
                var len = Vector3.Distance(childPos, rootPos);
                if (len > maxLen)
                {
                    maxLen = len;
                    tipPos = childPos;
                }
            }
        }

        if (maxLen <= 0f)
        {
            var dir = Vector3.Normalize(bRoot.BaseForward);
            var len = bRoot.Translation.Length();
            if (len <= 0f)
                len = 1f;
            tipPos = rootPos + dir * len;
        }

        var rootRole = DetermineRole(bRoot.Name);
        BonesDict[index] = new IkBone(index, bRoot.Name, rootRole, tipPos, bRoot.Rotation, bRoot.BaseForward, bRoot.BaseUp, isEffector);
    }

    private static void RegisterChainBones(IkInfo ikInfo)
    {
        if (_modelBones == null)
            return;

        if (!BonesDict.ContainsKey(ikInfo.Target))
            RegisterIkBone(ikInfo.Target, _modelBones[ikInfo.Target], true);

        foreach (var link in ikInfo.Links)
        {
            if (!BonesDict.ContainsKey(link.BoneIndex))
                RegisterIkBone(link.BoneIndex, _modelBones[link.BoneIndex], true);
        }
    }

    public static void SolveFootIk(int ikRootIndex, int effectorIndex, Vector3 target)
    {
        if (_modelBones == null)
            return;
        if (ikRootIndex < 0 || ikRootIndex >= _modelBones.Count)
            return;

        var rootBone = _modelBones[ikRootIndex];
        var ik = rootBone.Ik;
        if (ik == null)
            return;

        var chainFull = new List<int> { ik.Target };
        foreach (var link in ik.Links)
            chainFull.Add(link.BoneIndex);

        int start = chainFull.IndexOf(effectorIndex);
        if (start < 0)
        {
            Console.WriteLine($"[IK] effector index {effectorIndex} not in chain of {rootBone.Name}");
            return;
        }

        var chain = chainFull.GetRange(start, chainFull.Count - start);

        Console.WriteLine($"[IK] SolveFootIk root={rootBone.Name} effector={_modelBones[effectorIndex].Name} target={target}");

        for (int iter = 0; iter < Math.Max(1, ik.Iterations); iter++)
        {
            for (int c = 0; c < chain.Count; c++)
            {
                int idx = chain[c];
                if (!BonesDict.TryGetValue(idx, out var ikb))
                    continue;

                var jointPos = ikb.Position;
                var endPos = BonesDict[effectorIndex].Position;
                var toEnd = Vector3.Normalize(endPos - jointPos);
                var toTarget = Vector3.Normalize(target - jointPos);
                var axis = Vector3.Cross(toEnd, toTarget);
                // モデル読込時に Z 軸を反転して右手系へ変換しているため、
                // クロス積の結果も Z 成分を再反転して右手系の回転方向を保つ
                axis.Z = -axis.Z;
                if (axis.LengthSquared() < 1e-6f)
                    continue;
                axis = Vector3.Normalize(axis);
                var angle = MathF.Acos(Math.Clamp(Vector3.Dot(toEnd, toTarget), -1f, 1f));
                var rot = Quaternion.CreateFromAxisAngle(axis, angle);
                var bd = _modelBones[idx];
                bd.Rotation = Quaternion.Normalize(rot * bd.Rotation);

                if (bd.HasRotationLimit)
                {
                    var euler = QuaternionToEuler(bd.Rotation);
                    euler = ClampEuler(euler, bd.MinRotationDeg, bd.MaxRotationDeg);
                    bd.Rotation = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
                }

                ikb.Rotation = bd.Rotation;
                if (SetBoneRotation != null)
                {
                    var eulerDeg = QuaternionToEuler(bd.Rotation) * Rad2Deg;
                    // OpenGL ビュー空間は "前方 = -Z" のため、回転結果をレンダラーへ渡す際も Z を反転
                    SetBoneRotation(idx, eulerDeg.ToOpenTK(flipZ: true));
                }

                if (GetBonePositionFunc != null)
                {
                    var worldPos = GetBonePositionFunc(effectorIndex);
                    var modelPos = ToModelSpaceFunc != null ? ToModelSpaceFunc(worldPos) : worldPos;
                    if (BonesDict.TryGetValue(effectorIndex, out var eff))
                        eff.Position = modelPos;
                }
            }
        }

        if (GetBonePositionFunc != null)
        {
            var updateBones = new List<int>(chainFull);
            if (!updateBones.Contains(ikRootIndex))
                updateBones.Add(ikRootIndex);
            foreach (var idx in updateBones)
            {
                var worldPos = GetBonePositionFunc(idx);
                var modelPos = ToModelSpaceFunc != null ? ToModelSpaceFunc(worldPos) : worldPos;
                if (BonesDict.TryGetValue(idx, out var ikb))
                    ikb.Position = modelPos;
            }
        }
    }

    private const float Deg2Rad = MathF.PI / 180f;
    private const float Rad2Deg = 180f / MathF.PI;

    private static Vector3 QuaternionToEuler(Quaternion q)
    {
        var ysqr = q.Y * q.Y;

        var t0 = +2.0f * (q.W * q.X + q.Y * q.Z);
        var t1 = +1.0f - 2.0f * (q.X * q.X + ysqr);
        var pitch = MathF.Atan2(t0, t1);

        var t2 = +2.0f * (q.W * q.Y - q.Z * q.X);
        t2 = Math.Clamp(t2, -1f, 1f);
        var yaw = MathF.Asin(t2);

        var t3 = +2.0f * (q.W * q.Z + q.X * q.Y);
        var t4 = +1.0f - 2.0f * (ysqr + q.Z * q.Z);
        var roll = MathF.Atan2(t3, t4);

        return new Vector3(pitch, yaw, roll);
    }

    private static Vector3 ClampEuler(Vector3 eulerRad, Vector3 minDeg, Vector3 maxDeg)
    {
        var min = minDeg * Deg2Rad;
        var max = maxDeg * Deg2Rad;
        return Vector3.Clamp(eulerRad, min, max);
    }

    // レンダラーから提供された情報を用いてボーン選択を行う
    public static int PickBone(float screenX, float screenY)
    {
        if (PickFunc == null || GetBonePositionFunc == null || GetCameraPositionFunc == null)
            return -1;

        if (_selectedBoneIndex >= 0 && BonesDict.TryGetValue(_selectedBoneIndex, out var prev))
            prev.IsSelected = false;

        int idx = PickFunc(screenX, screenY);
        _selectedBoneIndex = idx;
        if (idx >= 0)
        {
            if (BonesDict.TryGetValue(idx, out var sel))
                sel.IsSelected = true;

            var bonePos = GetBonePositionFunc(idx);
            var camPos = GetCameraPositionFunc();
            if (ToModelSpaceFunc != null)
            {
                // レンダラー提供の WorldToModel で座標系を揃える
                bonePos = ToModelSpaceFunc(bonePos);
                camPos = ToModelSpaceFunc(camPos);
            }
            var normal = Vector3.Normalize(camPos - bonePos);
            _dragPlane = new Plane(normal, -Vector3.Dot(normal, bonePos));
        }
        InvalidateViewer?.Invoke();
        return idx;
    }

    public static Vector3? IntersectDragPlane((Vector3 Origin, Vector3 Direction) ray)
    {
        if (_selectedBoneIndex < 0)
        {
            Console.WriteLine("[IK] IntersectDragPlane called with no selected bone");
            return null;
        }

        var origin = ray.Origin;
        var dir = ray.Direction;
        if (ToModelSpaceFunc != null)
        {
            // レイの始点・方向ともにモデル座標系へ変換する
            var originModel = ToModelSpaceFunc(origin);
            var dirEnd = ToModelSpaceFunc(origin + dir);
            dir = Vector3.Normalize(dirEnd - originModel);
            origin = originModel;
        }

        var denom = Vector3.Dot(_dragPlane.Normal, dir);
        if (System.Math.Abs(denom) < 1e-6f)
        {
            Console.WriteLine($"[IK] IntersectDragPlane ray parallel to plane (denom={denom})");
            return null;
        }

        var t = -(Vector3.Dot(_dragPlane.Normal, origin) + _dragPlane.D) / denom;
        if (t < 0)
        {
            Console.WriteLine($"[IK] IntersectDragPlane intersection behind ray origin (t={t})");
            return null;
        }

        return origin + dir * t;
    }

    public static void UpdateTarget(int boneIndex, Vector3 position)
    {
        try
        {
            if (!BonesDict.TryGetValue(boneIndex, out var bone))
                return;

            Console.WriteLine($"[IK] UpdateTarget {bone.Name} -> {position}");

            if (_modelBones != null)
            {
                int current = _modelBones[bone.PmxBoneIndex].Parent;
                int ikRoot = -1;
                while (current >= 0)
                {
                    var bd = _modelBones[current];
                    if (bd.Ik != null)
                    {
                        ikRoot = current;
                        break;
                    }
                    current = bd.Parent;
                }

                if (ikRoot >= 0)
                {
                    Console.WriteLine($"[IK] root={_modelBones[ikRoot].Name} effector={bone.Name}");
                    SolveFootIk(ikRoot, bone.PmxBoneIndex, position);

                    if (GetBonePositionFunc != null)
                    {
                        var worldPos = GetBonePositionFunc(bone.PmxBoneIndex);
                        bone.Position = ToModelSpaceFunc != null ? ToModelSpaceFunc(worldPos) : worldPos;
                    }
                }
            }
            InvalidateViewer?.Invoke();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            throw;
        }
    }

    public static void ReleaseSelection()
    {
        if (_selectedBoneIndex >= 0 && BonesDict.TryGetValue(_selectedBoneIndex, out var prev))
            prev.IsSelected = false;
        _selectedBoneIndex = -1;
        InvalidateViewer?.Invoke();
    }

    public static void Clear()
    {
        ReleaseSelection();
        BonesDict.Clear();
        _modelBones = null;
    }

    private static BoneRole DetermineRole(string name)
    {
        if (name.Contains("足首", StringComparison.Ordinal))
            return BoneRole.Ankle;
        if (name.Contains("ひざ", StringComparison.Ordinal) || name.Contains("膝", StringComparison.Ordinal))
            return BoneRole.Knee;
        return BoneRole.None;
    }

}

