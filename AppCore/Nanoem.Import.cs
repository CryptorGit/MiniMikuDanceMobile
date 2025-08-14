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
        public int Category;
        public int Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IKConstraintLink
    {
        public int BoneIndex;
        public int HasLimit;
        public float LowerLimitX;
        public float LowerLimitY;
        public float LowerLimitZ;
        public float UpperLimitX;
        public float UpperLimitY;
        public float UpperLimitZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IKConstraintInfo
    {
        public int TargetBoneIndex;
        public float AngleLimit;
        public int Iterations;
        public int LinkCount;
        public IntPtr Links;
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
    private static extern void nanoemModelGetIKConstraintInfo(IntPtr model, uint boneIndex, out IKConstraintInfo info);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelIOFree(IntPtr ptr);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelDestroy(IntPtr model);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemMotionImportVMD(byte[] bytes, UIntPtr length, IntPtr factory, out int status);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemMotionDestroy(IntPtr motion);

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

    public static IKConstraintInfo ModelGetIKConstraintInfo(IntPtr model, uint boneIndex)
    {
        nanoemModelGetIKConstraintInfo(model, boneIndex, out var info);
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

    public static IntPtr MotionImportVmd(byte[] bytes, out int status)
    {
        return nanoemMotionImportVMD(bytes, (UIntPtr) bytes.Length, IntPtr.Zero, out status);
    }

    public static void MotionDestroy(IntPtr motion)
    {
        nanoemMotionDestroy(motion);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MaterialInfo
    {
        public IntPtr Name;
        public IntPtr EnglishName;
        public float DiffuseR;
        public float DiffuseG;
        public float DiffuseB;
        public float DiffuseA;
        public float SpecularR;
        public float SpecularG;
        public float SpecularB;
        public float SpecularA;
        public float AmbientR;
        public float AmbientG;
        public float AmbientB;
        public float AmbientA;
        public int TextureIndex;
    }

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nanoemModelGetTextureCount(IntPtr model);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelGetTexturePathAt(IntPtr model, uint index);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nanoemModelGetMaterialCount(IntPtr model);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelGetMaterialInfo(IntPtr model, uint index, out MaterialInfo info);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern float nanoemModelGetMorphInitialWeight(IntPtr model, uint index);

    public static uint ModelGetTextureCount(IntPtr model)
    {
        return nanoemModelGetTextureCount(model);
    }

    public static string ModelGetTexturePath(IntPtr model, uint index)
    {
        var ptr = nanoemModelGetTexturePathAt(model, index);
        return PtrToStringAndFree(ptr);
    }

    public static uint ModelGetMaterialCount(IntPtr model)
    {
        return nanoemModelGetMaterialCount(model);
    }

    public static MaterialInfo ModelGetMaterialInfo(IntPtr model, uint index)
    {
        nanoemModelGetMaterialInfo(model, index, out var info);
        return info;
    }

    public static float ModelGetMorphInitialWeight(IntPtr model, uint index)
    {
        return nanoemModelGetMorphInitialWeight(model, index);
    }
}
