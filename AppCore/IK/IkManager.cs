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
    public static System.Action? RecalculateWorldMatricesFunc { get; set; }
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
        var rootRole = DetermineRole(bRoot.Name);
        BonesDict[index] = new IkBone(index, bRoot.Name, rootRole, rootPos, bRoot.Rotation, bRoot.BaseForward, bRoot.BaseUp, isEffector);
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
            {
                var bd = _modelBones[link.BoneIndex];
                var role = DetermineRole(bd.Name);
                var isEffector = role == BoneRole.Knee || role == BoneRole.Waist;
                RegisterIkBone(link.BoneIndex, bd, isEffector);
            }

            // 末端ボーンから親を辿り、腰ボーンを登録
            int parent = _modelBones[link.BoneIndex].Parent;
            while (parent >= 0)
            {
                if (!BonesDict.ContainsKey(parent))
                {
                    var parentBd = _modelBones[parent];
                    if (DetermineRole(parentBd.Name) == BoneRole.Waist)
                    {
                        RegisterIkBone(parent, parentBd, true);
                        break;
                    }
                }
                else if (BonesDict[parent].Role == BoneRole.Waist)
                {
                    break;
                }
                parent = _modelBones[parent].Parent;
            }
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

        // chainFull の先頭に含まれていない親ボーンを IK ルートまで遡って挿入する
        if (start > 0)
        {
            int insertCount = 0;
            int parent = _modelBones[chainFull[0]].Parent;
            while (parent >= 0 && parent != ikRootIndex)
            {
                if (!chainFull.Contains(parent))
                {
                    chainFull.Insert(0, parent);
                    insertCount++;
                }
                parent = _modelBones[parent].Parent;
            }
            if (parent == ikRootIndex && !chainFull.Contains(ikRootIndex))
            {
                chainFull.Insert(0, ikRootIndex);
                insertCount++;
            }
            start += insertCount;
        }

        var chain = chainFull.GetRange(start, chainFull.Count - start);

        var updateBones = new List<int>(chainFull);
        if (!updateBones.Contains(ikRootIndex))
            updateBones.Add(ikRootIndex);

        var visited = new HashSet<int>(updateBones);
        for (int i = 0; i < updateBones.Count; i++)
        {
            int parent = updateBones[i];
            for (int j = 0; j < _modelBones.Count; j++)
            {
                if (_modelBones[j].Parent == parent && visited.Add(j))
                    updateBones.Add(j);
            }
        }

        RecalculateWorldMatricesFunc?.Invoke();
        if (GetBonePositionFunc != null)
        {
            foreach (var bIdx in updateBones)
            {
                var worldPos = GetBonePositionFunc(bIdx);
                var modelPos = ToModelSpaceFunc != null ? ToModelSpaceFunc(worldPos) : worldPos;
                if (BonesDict.TryGetValue(bIdx, out var b))
                    b.Position = modelPos;
            }
        }

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

                    // OpenGL ビュー空間は "前方 = -Z" のため、回転結果をレンダラーへ渡す際も Z を反転
                    SetBoneRotation(idx, OpenTK.Mathematics.Vector3.Zero);
                }

                RecalculateWorldMatricesFunc?.Invoke();
                if (GetBonePositionFunc != null)
                {
                    foreach (var bIdx in updateBones)
                    {
                        var worldPos = GetBonePositionFunc(bIdx);
                        var modelPos = ToModelSpaceFunc != null ? ToModelSpaceFunc(worldPos) : worldPos;
                        if (BonesDict.TryGetValue(bIdx, out var b))
                            b.Position = modelPos;
                    }
                }
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
            boneIndex = FindEffectorForBone(boneIndex);
            _selectedBoneIndex = boneIndex;
            if (!BonesDict.TryGetValue(boneIndex, out var bone))
                return;

            if (!bone.IsEffector)
            {
                Console.WriteLine($"[IK] UpdateTarget ignored: {bone.Name} is not an IK effector");
                return;
            }

            Console.WriteLine($"[IK] UpdateTarget {bone.Name} -> {position}");

            if (_modelBones != null)
            {
                int ikRoot = FindIkRootForEffector(bone.PmxBoneIndex);
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
            // Recompute drag plane to stay orthogonal to current view and pass through updated bone position
            try
            {
                if (GetBonePositionFunc != null && GetCameraPositionFunc != null)
                {
                    var bonePos = GetBonePositionFunc(_selectedBoneIndex);
                    var camPos = GetCameraPositionFunc();
                    if (ToModelSpaceFunc != null)
                    {
                        bonePos = ToModelSpaceFunc(bonePos);
                        camPos = ToModelSpaceFunc(camPos);
                    }
                    var normal = Vector3.Normalize(camPos - bonePos);
                    _dragPlane = new Plane(normal, -Vector3.Dot(normal, bonePos));
                }
            }
            catch { /* ignore plane refresh errors */ }

            InvalidateViewer?.Invoke();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            throw;
        }
    }

    private static int FindEffectorForBone(int boneIndex)
    {
        if (_modelBones == null || boneIndex < 0)
            return boneIndex;

        if (BonesDict.TryGetValue(boneIndex, out var b) && b.IsEffector)
            return boneIndex;

        for (int i = 0; i < _modelBones.Count; i++)
        {
            var ik = _modelBones[i].Ik;
            if (ik == null)
                continue;

            if (ik.Target == boneIndex || i == boneIndex)
                return ik.Target;

            foreach (var link in ik.Links)
            {
                if (link.BoneIndex == boneIndex)
                    return ik.Target;
            }
        }

        return boneIndex;
    }

    private static int FindIkRootForEffector(int effectorIndex)
    {
        if (_modelBones == null)
            return -1;
        for (int i = 0; i < _modelBones.Count; i++)
        {
            var ik = _modelBones[i].Ik;
            if (ik == null)
                continue;
            if (ik.Target == effectorIndex)
                return i;
            foreach (var link in ik.Links)
            {
                if (link.BoneIndex == effectorIndex)
                    return i;
            }
        }
        return -1;
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
        if (name.Contains("腰", StringComparison.Ordinal))
            return BoneRole.Waist;
        return BoneRole.None;
    }

}

