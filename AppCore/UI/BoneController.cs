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
}
