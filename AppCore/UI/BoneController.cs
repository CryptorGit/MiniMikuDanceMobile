using System;
using System.Numerics;

namespace MiniMikuDance.UI;

public static class BoneController
{
    public static Matrix4x4 GetTransform(IntPtr model, int index, Matrix4x4 fallback)
    {
        if (model != IntPtr.Zero)
        {
            var bone = NanoemBone.nanoemModelGetBoneObject(model, index);
            if (bone != IntPtr.Zero)
            {
                NanoemBone.nanoemModelBoneGetTransformMatrix(bone, out var m);
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
                NanoemBone.nanoemModelBoneSetTransformMatrix(bone, in value);
            }
        }
    }

    public static void Translate(IntPtr model, int index, Vector3 delta)
    {
        var current = GetTransform(model, index, Matrix4x4.Identity);
        var m = Matrix4x4.CreateTranslation(delta) * current;
        SetTransform(model, index, m);
    }
}
