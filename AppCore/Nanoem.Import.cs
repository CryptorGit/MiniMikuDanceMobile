using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static partial class Nanoem
{
    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemModelImportPMX(byte[] bytes, UIntPtr length, IntPtr factory, out int status);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nanoemModelGetVertexCount(IntPtr model);

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

    public static void ModelDestroy(IntPtr model)
    {
        nanoemModelDestroy(model);
    }
}
