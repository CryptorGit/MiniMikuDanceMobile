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

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelMorphGetVertexOffsets(IntPtr model, int index, out UIntPtr count);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelMorphGetUVOffsets(IntPtr model, int index, out UIntPtr count);

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
}
