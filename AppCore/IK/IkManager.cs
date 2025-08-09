using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.IK;

public enum IkBoneType
{
    Head,
    Chest,
    Hip,
    LeftUpperArm,
    LeftLowerArm,
    LeftHand,
    RightUpperArm,
    RightLowerArm,
    RightHand,
    LeftUpperLeg,
    RightUpperLeg
}

public static class IkManager
{
    private static readonly Dictionary<IkBoneType, string> BoneNames = new()
    {
        { IkBoneType.Head, "head" },
        { IkBoneType.Chest, "chest" },
        { IkBoneType.Hip, "hips" },
        { IkBoneType.LeftUpperArm, "leftUpperArm" },
        { IkBoneType.LeftLowerArm, "leftLowerArm" },
        { IkBoneType.LeftHand, "leftHand" },
        { IkBoneType.RightUpperArm, "rightUpperArm" },
        { IkBoneType.RightLowerArm, "rightLowerArm" },
        { IkBoneType.RightHand, "rightHand" },
        { IkBoneType.LeftUpperLeg, "leftUpperLeg" },
        { IkBoneType.RightUpperLeg, "rightUpperLeg" }
    };

    private static readonly Dictionary<IkBoneType, IkBone> BonesDict = new();
    private static readonly Dictionary<int, IkBone> BoneIndexDict = new();

    // レンダラーから提供される各種処理を委譲用デリゲートとして保持
    public static System.Func<float, float, int>? PickFunc { get; set; }
    public static System.Func<int, Vector3>? GetBonePositionFunc { get; set; }
    public static System.Func<Vector3>? GetCameraPositionFunc { get; set; }

    private static int _selectedBoneIndex = -1;
    private static Plane _dragPlane;

    public static int SelectedBoneIndex => _selectedBoneIndex;

    public static IReadOnlyDictionary<IkBoneType, IkBone> Bones => BonesDict;

    public static void Initialize(IReadOnlyList<BoneData> modelBones)
    {
        if (BonesDict.Count > 0) return;
        foreach (var kv in BoneNames)
        {
            int idx = FindBoneIndex(modelBones, kv.Value);
            if (idx >= 0)
            {
                var b = modelBones[idx];
                var ik = new IkBone(idx, b.BindMatrix.Translation, Quaternion.Identity);
                BonesDict[kv.Key] = ik;
                BoneIndexDict[idx] = ik;
            }
        }
    }

    private static int FindBoneIndex(IReadOnlyList<BoneData> bones, string name)
    {
        for (int i = 0; i < bones.Count; i++)
        {
            if (bones[i].Name == name) return i;
        }
        return -1;
    }

    public static IkBone? Get(IkBoneType type)
    {
        return BonesDict.TryGetValue(type, out var bone) ? bone : null;
    }

    public static void Update(IkBoneType type, Vector3 position, Quaternion rotation)
    {
        if (BonesDict.TryGetValue(type, out var bone))
        {
            bone.Position = position;
            bone.Rotation = rotation;
        }
    }

    // レンダラーから提供された情報を用いてボーン選択を行う
    public static int PickBone(float screenX, float screenY)
    {
        if (PickFunc == null || GetBonePositionFunc == null || GetCameraPositionFunc == null)
            return -1;

        int idx = PickFunc(screenX, screenY);
        _selectedBoneIndex = idx;
        if (idx >= 0)
        {
            var bonePos = GetBonePositionFunc(idx);
            var camPos = GetCameraPositionFunc();
            var normal = Vector3.Normalize(camPos - bonePos);
            _dragPlane = new Plane(bonePos, normal);
        }
        return idx;
    }

    public static Vector3? IntersectDragPlane((Vector3 Origin, Vector3 Direction) ray)
    {
        if (_selectedBoneIndex < 0)
            return null;

        var denom = Vector3.Dot(_dragPlane.Normal, ray.Direction);
        if (System.Math.Abs(denom) < 1e-6f)
            return null;

        var t = -(Vector3.Dot(_dragPlane.Normal, ray.Origin) + _dragPlane.D) / denom;
        if (t < 0)
            return null;

        return ray.Origin + ray.Direction * t;
    }

    public static void UpdateTarget(int boneIndex, Vector3 position)
    {
        if (BoneIndexDict.TryGetValue(boneIndex, out var bone))
        {
            bone.Position = position;
        }
    }

    public static void ReleaseSelection()
    {
        _selectedBoneIndex = -1;
    }

    public static void Clear()
    {
        BonesDict.Clear();
        BoneIndexDict.Clear();
        ReleaseSelection();
    }
}
