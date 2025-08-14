using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance.Import;

public static class NativeModelImporter
{
    private const string LibraryName = "nanoem";

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelImportPMX(byte[] bytes, UIntPtr length, IntPtr factory, out int status);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern UIntPtr nanoemModelGetVertexCount(IntPtr model);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelDestroy(IntPtr model);

    public static int GetVertexCount(ReadOnlySpan<byte> data)
    {
        int status;
        IntPtr factory = IntPtr.Zero;
        IntPtr model = nanoemModelImportPMX(data.ToArray(), (UIntPtr)data.Length, factory, out status);
        if (model == IntPtr.Zero || status != 0)
        {
            throw new InvalidOperationException($"PMX import failed: {status}");
        }
        var count = (int)nanoemModelGetVertexCount(model);
        nanoemModelDestroy(model);
        return count;
    }
}

public static class NativeMotionImporter
{
    private const string LibraryName = "nanoem";

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemMotionImportVMD(byte[] bytes, UIntPtr length, IntPtr factory, out int status);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern UIntPtr nanoemMotionGetBoneKeyframeCount(IntPtr motion);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemMotionDestroy(IntPtr motion);

    public static int GetBoneKeyframeCount(ReadOnlySpan<byte> data)
    {
        int status;
        IntPtr factory = IntPtr.Zero;
        IntPtr motion = nanoemMotionImportVMD(data.ToArray(), (UIntPtr)data.Length, factory, out status);
        if (motion == IntPtr.Zero || status != 0)
        {
            throw new InvalidOperationException($"VMD import failed: {status}");
        }
        var count = (int)nanoemMotionGetBoneKeyframeCount(motion);
        nanoemMotionDestroy(motion);
        return count;
    }
}
