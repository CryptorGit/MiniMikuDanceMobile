using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static class Nanoem
{
    private const string NativeLibName = "nanoem";

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr nanoemGetVersionString();

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int nanoemAdd(int left, int right);

    public static string GetVersionString()
    {
        var ptr = nanoemGetVersionString();
        return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
    }

    public static int Add(int left, int right)
    {
        return nanoemAdd(left, right);
    }
}
