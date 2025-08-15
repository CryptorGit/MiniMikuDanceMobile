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
    public static System.Func<int, OpenTK.Mathematics.Vector3>? GetBoneRotationFunc { get; set; }
    public static System.Func<Vector3>? GetCameraPositionFunc { get; set; }
    public static System.Action<int, OpenTK.Mathematics.Vector3>? SetBoneTranslation { get; set; }
    public static System.Action<int, OpenTK.Mathematics.Vector3>? SetBoneRotation { get; set; }
    public static System.Func<Vector3, Vector3>? ToModelSpaceFunc { get; set; }
    public static System.Func<Vector3, Vector3>? ToWorldSpaceFunc { get; set; }
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

    private static void RegisterIkBone(int index, BoneData bRoot)
    {
        var rootPos = Vector3.Transform(Vector3.Zero, bRoot.BindMatrix);
        var rootRole = DetermineRole(bRoot.Name);
        BonesDict[index] = new IkBone(index, bRoot.Name, rootRole, rootPos, bRoot.Rotation, bRoot.BaseForward, bRoot.BaseUp);
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
            return null;

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
            return null;

        var t = -(Vector3.Dot(_dragPlane.Normal, origin) + _dragPlane.D) / denom;
        if (t < 0)
            return null;

        return origin + dir * t;
    }

    public static void UpdateTarget(int boneIndex, Vector3 position)
    {
        try
        {
            if (!BonesDict.TryGetValue(boneIndex, out var bone))
                return;

            bone.Position = position;

            if (SetBoneTranslation != null)
            {
                var worldPos = ToWorldSpaceFunc != null ? ToWorldSpaceFunc(bone.Position) : bone.Position;
                SetBoneTranslation(bone.PmxBoneIndex, worldPos.ToOpenTK());
            }

            SolveIk(boneIndex);

            InvalidateViewer?.Invoke();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            throw;
        }
    }

    /// <summary>
    /// IK チェーンを解決し、ボーンに変更があった場合は true を返す。
    /// </summary>
    public static bool Solve()
    {
        if (_modelBones == null)
            return false;

        bool updated = false;
        foreach (var kv in BonesDict)
            updated |= SolveIk(kv.Key);

        return updated;
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
    }

    private static BoneRole DetermineRole(string name)
    {
        if (name.Contains("足首", StringComparison.Ordinal))
            return BoneRole.Ankle;
        if (name.Contains("ひざ", StringComparison.Ordinal) || name.Contains("膝", StringComparison.Ordinal))
            return BoneRole.Knee;
        return BoneRole.None;
    }

    public static void SolveAll()
    {
        if (_modelBones == null)
            return;
        foreach (var kv in BonesDict)
            SolveIk(kv.Key);
    }

    private static bool SolveIk(int boneIndex)
    {
        if (_modelBones == null || GetBonePositionFunc == null || GetBoneRotationFunc == null || SetBoneRotation == null)
            return false;
        if (boneIndex < 0 || boneIndex >= _modelBones.Count)
            return false;

        var info = _modelBones[boneIndex].Ik;
        if (info == null)
            return false;

        var targetPos = BonesDict.TryGetValue(boneIndex, out var ikb) ? ikb.Position : Vector3.Zero;
        if (ToWorldSpaceFunc != null)
            targetPos = ToWorldSpaceFunc(targetPos);

        bool changed = false;
        for (int i = 0; i < info.Iterations; i++)
        {
            for (int j = 0; j < info.Links.Count; j++)
            {
                var link = info.Links[j];
                int linkIdx = link.BoneIndex;
                var linkPos = GetBonePositionFunc(linkIdx);
                var effPos = GetBonePositionFunc(info.Target);
                var toEff = Vector3.Normalize(effPos - linkPos);
                var toTarget = Vector3.Normalize(targetPos - linkPos);
                float cos = Math.Clamp(Vector3.Dot(toEff, toTarget), -1f, 1f);
                if (cos > 0.99999f)
                    continue;

                float angle = MathF.Acos(cos) * info.ControlWeight;
                var axis = Vector3.Normalize(Vector3.Cross(toEff, toTarget));
                if (axis.LengthSquared() < 1e-6f)
                    continue;

                var delta = Quaternion.CreateFromAxisAngle(axis, angle);
                var currentEuler = GetBoneRotationFunc(linkIdx);
                var currentQuat = currentEuler.ToNumerics().FromEulerDegrees();
                var newQuat = delta * currentQuat;
                var newEuler = QuaternionToEulerDegrees(newQuat);
                if (link.HasLimit)
                {
                    var min = link.MinAngle * (180f / MathF.PI);
                    var max = link.MaxAngle * (180f / MathF.PI);
                    newEuler.X = Math.Clamp(newEuler.X, min.X, max.X);
                    newEuler.Y = Math.Clamp(newEuler.Y, min.Y, max.Y);
                    newEuler.Z = Math.Clamp(newEuler.Z, min.Z, max.Z);
                }

                SetBoneRotation(linkIdx, newEuler.ToOpenTK());
                changed = true;
            }
        }

        return changed;
    }

    private static Vector3 QuaternionToEulerDegrees(Quaternion q)
    {
        var m = Matrix4x4.CreateFromQuaternion(q);
        float x = MathF.Asin(Math.Clamp(m.M32, -1f, 1f));
        float cx = MathF.Cos(x);
        float y, z;
        if (MathF.Abs(cx) > 1e-6f)
        {
            y = MathF.Atan2(-m.M31, m.M33);
            z = MathF.Atan2(-m.M12, m.M22);
        }
        else
        {
            y = 0f;
            z = MathF.Atan2(m.M21, m.M11);
        }
        return new Vector3(x, y, z) * (180f / MathF.PI);
    }

}

