using System;
using System.Numerics;

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

    public static Matrix4x4 GetTransform(IntPtr model, int index, Matrix4x4 fallback)
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
                var translation = value.Translation;
                NanoemBone.nanoemModelBoneSetTranslation(bone, in translation);
                var rotation = Quaternion.CreateFromRotationMatrix(value);
                NanoemBone.nanoemModelBoneSetOrientation(bone, in rotation);
            }
        }
    }
}
