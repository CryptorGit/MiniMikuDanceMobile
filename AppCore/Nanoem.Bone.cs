using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static class NanoemBone
{
    private const string NativeLibName = "nanoem";

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr nanoemModelGetBoneObject(IntPtr model, int index);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint nanoemModelGetBoneCount(IntPtr model);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void nanoemModelBoneSetTranslation(IntPtr bone, in Vector3 value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void nanoemModelBoneGetTranslation(IntPtr bone, out Vector3 value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void nanoemModelBoneSetOrientation(IntPtr bone, in Quaternion value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void nanoemModelBoneGetOrientation(IntPtr bone, out Quaternion value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void nanoemModelBoneGetTransform(IntPtr bone, out Matrix4x4 value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelGetBoneName(IntPtr model, int index);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int nanoemModelBoneGetParent(IntPtr bone);

    public static string? GetName(IntPtr model, int index)
    {
        var ptr = nanoemModelGetBoneName(model, index);
        return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
    }

    public static int GetParent(IntPtr bone) => nanoemModelBoneGetParent(bone);
}
