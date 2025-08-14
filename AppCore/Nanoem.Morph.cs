using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static partial class Nanoem
{
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

    public static int GetMorphCount(IntPtr model) => nanoemModelGetMorphCount(model);

    public static string GetMorphName(IntPtr model, int index)
    {
        var ptr = nanoemModelGetMorphName(model, index);
        return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
    }

    public static int GetMorphCategory(IntPtr model, int index) => nanoemModelGetMorphCategory(model, index);

    public static int GetMorphType(IntPtr model, int index) => nanoemModelGetMorphType(model, index);

    public static void SetMorphWeight(IntPtr model, int index, float weight) => nanoemModelSetMorphWeight(model, index, weight);
}
