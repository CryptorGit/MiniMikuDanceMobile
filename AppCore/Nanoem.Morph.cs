using System;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static partial class Nanoem
{
    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelSetMorphWeight(IntPtr model, int index, float weight);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemModelUpdateMorph(IntPtr model);

    public static void SetMorphWeight(IntPtr model, int index, float weight) => nanoemModelSetMorphWeight(model, index, weight);

    public static void ModelUpdateMorph(IntPtr model) => nanoemModelUpdateMorph(model);
}
