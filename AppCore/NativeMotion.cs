using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static class NativeMotion
{
    private const string LibraryName = "nanoem";

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr nanoem__motion__motion__unpack(IntPtr allocator, UIntPtr len, byte[] data);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void nanoem__motion__motion__free_unpacked(IntPtr message, IntPtr allocator);
}
