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
}
