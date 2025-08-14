using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static class Nanoem
{
    private const string NativeLibName = "nanoem";

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemGetVersionString();

    public static string GetVersionString()
    {
        var ptr = nanoemGetVersionString();
        return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
    }
}
