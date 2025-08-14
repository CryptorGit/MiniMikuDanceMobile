using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static partial class Nanoem
{
    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingInitialize")]
    private static extern void RenderingInitializeNative(int width, int height);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingUpdateFrame")]
    private static extern void RenderingUpdateFrameNative();

    public static void RenderingInitialize(int width, int height)
    {
        RenderingInitializeNative(width, height);
    }

    public static void RenderingUpdateFrame()
    {
        RenderingUpdateFrameNative();
    }
}
