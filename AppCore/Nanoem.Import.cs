using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static partial class Nanoem
{
    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelImportPMX(byte[] bytes, UIntPtr length, IntPtr factory, out int status);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nanoemModelGetVertexCount(IntPtr model);

    [StructLayout(LayoutKind.Sequential)]
    internal struct ModelInfo
    {
        public IntPtr Name;
        public IntPtr EnglishName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BoneInfo
    {
        public IntPtr Name;
        public IntPtr EnglishName;
        public int ParentBoneIndex;
        public float OriginX;
        public float OriginY;
        public float OriginZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MorphInfo
    {
        public IntPtr Name;
        public IntPtr EnglishName;
        public int Type;
    }

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelGetInfo(IntPtr model, out ModelInfo info);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nanoemModelGetBoneCount(IntPtr model);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelGetBoneInfo(IntPtr model, uint index, out BoneInfo info);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nanoemModelGetMorphCount(IntPtr model);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelGetMorphInfo(IntPtr model, uint index, out MorphInfo info);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelIOFree(IntPtr ptr);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelDestroy(IntPtr model);

    public static IntPtr ModelImportPmx(byte[] bytes, out int status)
    {
        return nanoemModelImportPMX(bytes, (UIntPtr)bytes.Length, IntPtr.Zero, out status);
    }

    public static uint ModelGetVertexCount(IntPtr model)
    {
        return nanoemModelGetVertexCount(model);
    }

    public static ModelInfo ModelGetInfo(IntPtr model)
    {
        nanoemModelGetInfo(model, out var info);
        return info;
    }

    public static uint ModelGetBoneCount(IntPtr model)
    {
        return nanoemModelGetBoneCount(model);
    }

    public static BoneInfo ModelGetBoneInfo(IntPtr model, uint index)
    {
        nanoemModelGetBoneInfo(model, index, out var info);
        return info;
    }

    public static uint ModelGetMorphCount(IntPtr model)
    {
        return nanoemModelGetMorphCount(model);
    }

    public static MorphInfo ModelGetMorphInfo(IntPtr model, uint index)
    {
        nanoemModelGetMorphInfo(model, index, out var info);
        return info;
    }

    public static string PtrToStringAndFree(IntPtr ptr)
    {
        string s = Marshal.PtrToStringUTF8(ptr)!;
        nanoemModelIOFree(ptr);
        return s;
    }

    public static void ModelDestroy(IntPtr model)
    {
        nanoemModelDestroy(model);
    }
}
