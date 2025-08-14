using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;
using MiniMikuDance.Util;

namespace MiniMikuDance.IK;

public static class IkManager
{
    private static readonly Dictionary<int, IkBone> BonesDict = new();

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

    public static void LoadPmxIkBones(IReadOnlyList<BoneData> modelBones, IReadOnlyList<int> ikBoneIndices)
    {
        Clear();
        Nanoem.InitializeIk(ikBoneIndices.Count);
        for (int c = 0; c < ikBoneIndices.Count; c++)
        {
            int idx = ikBoneIndices[c];
            RegisterIkBone(c, idx, modelBones[idx]);
        }
    }

    private static void RegisterIkBone(int constraintIndex, int index, BoneData bRoot)
    {
        var rootPos = Vector3.Transform(Vector3.Zero, bRoot.BindMatrix);
        BonesDict[index] = new IkBone(index, constraintIndex, bRoot.Name, rootPos);
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

            // ネイティブ IK ソルバーを呼び出してボーン位置を更新
            var pos = new float[] { position.X, position.Y, position.Z };
            Nanoem.SolveIk(bone.ConstraintIndex, boneIndex, pos);
            bone.Position = new Vector3(pos[0], pos[1], pos[2]);

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
}

