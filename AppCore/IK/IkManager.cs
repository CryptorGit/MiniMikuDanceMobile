using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;
using MiniMikuDance.Util;

namespace MiniMikuDance.IK;

public static class IkManager
{
    private static readonly Dictionary<int, IkBone> BonesDict = new();
    // IKアルゴリズム関連の処理を削除したため、ソルバーや固定軸の管理は不要

    // レンダラーから提供される各種処理を委譲用デリゲートとして保持
    public static System.Func<float, float, int>? PickFunc { get; set; }
    public static System.Func<int, Vector3>? GetBonePositionFunc { get; set; }
    public static System.Func<Vector3>? GetCameraPositionFunc { get; set; }
    public static System.Action<int, OpenTK.Mathematics.Vector3>? SetBoneTranslation { get; set; }
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
        // TODO: CCD や FABRIK などのアルゴリズムで IK を解決する
        // 現段階では未実装のため常に変更なしとする
        return false;
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

}

