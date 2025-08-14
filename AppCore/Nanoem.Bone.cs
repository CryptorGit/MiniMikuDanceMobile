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
    public static extern void nanoemModelBoneSetTransformMatrix(IntPtr bone, in Matrix4x4 value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void nanoemModelBoneGetTransformMatrix(IntPtr bone, out Matrix4x4 value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void nanoemModelBoneUpdate(IntPtr bone);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void nanoemModelBoneUpdateAll(IntPtr model);
}
