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
                BonesDict[kv.Key] = new IkBone(idx, b.BindMatrix.Translation, Quaternion.Identity);
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

    public static void Clear()
    {
        BonesDict.Clear();
    }
}
