using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.UI;

public static class BoneController
{
    public static Vector3 GetTranslation(IntPtr model, int index, Vector3 fallback)
    {
        if (model != IntPtr.Zero)
        {
            var bone = NanoemBone.nanoemModelGetBoneObject(model, index);
            if (bone != IntPtr.Zero)
            {
                NanoemBone.nanoemModelBoneGetTranslation(bone, out var v);
                return v;
            }
        }
        return fallback;
    }

    public static void SetTranslation(IntPtr model, int index, in Vector3 value)
    {
        if (model != IntPtr.Zero)
        {
            var bone = NanoemBone.nanoemModelGetBoneObject(model, index);
            if (bone != IntPtr.Zero)
            {
                NanoemBone.nanoemModelBoneSetTranslation(bone, in value);
            }
        }
    }

    public static void Translate(IntPtr model, int index, Vector3 delta)
    {
        var current = GetTranslation(model, index, Vector3.Zero);
        var next = current + delta;
        SetTranslation(model, index, in next);
    }

    public static Quaternion GetRotation(IntPtr model, int index, Quaternion fallback)
    {
        if (model != IntPtr.Zero)
        {
            var bone = NanoemBone.nanoemModelGetBoneObject(model, index);
            if (bone != IntPtr.Zero)
            {
                NanoemBone.nanoemModelBoneGetOrientation(bone, out var q);
                return q;
            }
        }
        return fallback;
    }

    public static void SetRotation(IntPtr model, int index, in Quaternion value)
    {
        if (model != IntPtr.Zero)
        {
            var bone = NanoemBone.nanoemModelGetBoneObject(model, index);
            if (bone != IntPtr.Zero)
            {
                NanoemBone.nanoemModelBoneSetOrientation(bone, in value);
            }
        }
    }

    public static Matrix4x4 GetWorldTransform(IntPtr model, int index, Matrix4x4 fallback)
    {
        if (model != IntPtr.Zero)
        {
            var bone = NanoemBone.nanoemModelGetBoneObject(model, index);
            if (bone != IntPtr.Zero)
            {
                NanoemBone.nanoemModelBoneGetTransform(bone, out var m);
                return m;
            }
        }
        return fallback;
    }

    public static void SetTransform(IntPtr model, int index, Matrix4x4 value)
    {
        if (model != IntPtr.Zero)
        {
            var bone = NanoemBone.nanoemModelGetBoneObject(model, index);
            if (bone != IntPtr.Zero)
            {
                Matrix4x4.Decompose(value, out _, out var rotation, out var translation);
                NanoemBone.nanoemModelBoneSetTranslation(bone, in translation);
                rotation = Quaternion.Normalize(rotation);
                NanoemBone.nanoemModelBoneSetOrientation(bone, in rotation);
            }
        }
    }

    public static bool ValidateBoneIndices(IntPtr model, IReadOnlyList<BoneData> bones)
    {
        if (model == IntPtr.Zero)
            return false;
        uint count = NanoemBone.nanoemModelGetBoneCount(model);
        if (bones.Count != count)
            return false;
        for (int i = 0; i < bones.Count; i++)
        {
            var bone = NanoemBone.nanoemModelGetBoneObject(model, i);
            if (bone == IntPtr.Zero)
                return false;
            var name = NanoemBone.GetName(model, i);
            int parent = NanoemBone.GetParent(bone);
            NanoemBone.nanoemModelBoneGetTranslation(bone, out var t);
            NanoemBone.nanoemModelBoneGetOrientation(bone, out var r);
            if (name != bones[i].Name || parent != bones[i].Parent || !t.Equals(bones[i].Translation) || !r.Equals(bones[i].Rotation))
                return false;
        }
        return true;
    }
}
