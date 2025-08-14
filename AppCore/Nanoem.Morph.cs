using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MiniMikuDance;

internal static class NanoemMorph
{
    private const string NativeLibName = "nanoem";

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int nanoemModelGetMorphCount(IntPtr model);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelGetMorphName(IntPtr model, int index);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int nanoemModelGetMorphCategory(IntPtr model, int index);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int nanoemModelGetMorphType(IntPtr model, int index);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelSetMorphWeight(IntPtr model, int index, float weight);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelUpdateMorph(IntPtr model);

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexOffset
    {
        public int VertexIndex;
        public float X;
        public float Y;
        public float Z;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UvOffset
    {
        public int VertexIndex;
        public float X;
        public float Y;
        public float Z;
        public float W;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BoneOffset
    {
        public int BoneIndex;
        public float TranslationX;
        public float TranslationY;
        public float TranslationZ;
        public float OrientationX;
        public float OrientationY;
        public float OrientationZ;
        public float OrientationW;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GroupOffset
    {
        public int MorphIndex;
        public float Weight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialOffset
    {
        public int MaterialIndex;
        public int OperationType;
        public float AmbientR;
        public float AmbientG;
        public float AmbientB;
        public float DiffuseR;
        public float DiffuseG;
        public float DiffuseB;
        public float DiffuseA;
        public float SpecularR;
        public float SpecularG;
        public float SpecularB;
        public float SpecularPower;
        public float EdgeColorR;
        public float EdgeColorG;
        public float EdgeColorB;
        public float EdgeColorA;
        public float EdgeSize;
        public float TextureBlendR;
        public float TextureBlendG;
        public float TextureBlendB;
        public float TextureBlendA;
        public float SphereTextureBlendR;
        public float SphereTextureBlendG;
        public float SphereTextureBlendB;
        public float SphereTextureBlendA;
        public float ToonTextureBlendR;
        public float ToonTextureBlendG;
        public float ToonTextureBlendB;
        public float ToonTextureBlendA;
    }

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelMorphGetVertexOffsets(IntPtr model, int index, out UIntPtr count);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelMorphGetUVOffsets(IntPtr model, int index, out UIntPtr count);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelMorphGetBoneOffsets(IntPtr model, int index, out UIntPtr count);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelMorphGetGroupOffsets(IntPtr model, int index, out UIntPtr count);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelMorphGetMaterialOffsets(IntPtr model, int index, out UIntPtr count);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelIOFree(IntPtr ptr);

    public static int GetMorphCount(IntPtr model) => nanoemModelGetMorphCount(model);

    public static string? GetMorphName(IntPtr model, int index)
    {
        var ptr = nanoemModelGetMorphName(model, index);
        return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
    }

    public static int GetMorphCategory(IntPtr model, int index) => nanoemModelGetMorphCategory(model, index);

    public static int GetMorphType(IntPtr model, int index) => nanoemModelGetMorphType(model, index);

    public static void SetMorphWeight(IntPtr model, int index, float weight) => nanoemModelSetMorphWeight(model, index, weight);

    public static void Update(IntPtr model) => nanoemModelUpdateMorph(model);

    public static VertexOffset[] GetVertexOffsets(IntPtr model, int index)
    {
        var ptr = nanoemModelMorphGetVertexOffsets(model, index, out var count);
        int n = (int) count;
        var result = new VertexOffset[n];
        if (ptr != IntPtr.Zero && n > 0)
        {
            var size = Marshal.SizeOf<VertexOffset>();
            for (int i = 0; i < n; i++)
            {
                result[i] = Marshal.PtrToStructure<VertexOffset>(ptr + i * size);
            }
            nanoemModelIOFree(ptr);
        }
        return result;
    }

    public static UvOffset[] GetUvOffsets(IntPtr model, int index)
    {
        var ptr = nanoemModelMorphGetUVOffsets(model, index, out var count);
        int n = (int) count;
        var result = new UvOffset[n];
        if (ptr != IntPtr.Zero && n > 0)
        {
            var size = Marshal.SizeOf<UvOffset>();
            for (int i = 0; i < n; i++)
            {
                result[i] = Marshal.PtrToStructure<UvOffset>(ptr + i * size);
            }
            nanoemModelIOFree(ptr);
        }
        return result;
    }

    public static BoneOffset[] GetBoneOffsets(IntPtr model, int index)
    {
        var ptr = nanoemModelMorphGetBoneOffsets(model, index, out var count);
        int n = (int) count;
        var result = new BoneOffset[n];
        if (ptr != IntPtr.Zero && n > 0)
        {
            var size = Marshal.SizeOf<BoneOffset>();
            for (int i = 0; i < n; i++)
            {
                result[i] = Marshal.PtrToStructure<BoneOffset>(ptr + i * size);
            }
            nanoemModelIOFree(ptr);
        }
        return result;
    }

    public static GroupOffset[] GetGroupOffsets(IntPtr model, int index)
    {
        var ptr = nanoemModelMorphGetGroupOffsets(model, index, out var count);
        int n = (int) count;
        var result = new GroupOffset[n];
        if (ptr != IntPtr.Zero && n > 0)
        {
            var size = Marshal.SizeOf<GroupOffset>();
            for (int i = 0; i < n; i++)
            {
                result[i] = Marshal.PtrToStructure<GroupOffset>(ptr + i * size);
            }
            nanoemModelIOFree(ptr);
        }
        return result;
    }

    public static MaterialOffset[] GetMaterialOffsets(IntPtr model, int index)
    {
        var ptr = nanoemModelMorphGetMaterialOffsets(model, index, out var count);
        int n = (int) count;
        var result = new MaterialOffset[n];
        if (ptr != IntPtr.Zero && n > 0)
        {
            var size = Marshal.SizeOf<MaterialOffset>();
            for (int i = 0; i < n; i++)
            {
                result[i] = Marshal.PtrToStructure<MaterialOffset>(ptr + i * size);
            }
            nanoemModelIOFree(ptr);
        }
        return result;
    }
}
