using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static partial class Nanoem
{
    private const string NativeLibName = "nanoem";

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingInitialize")]
    private static extern void RenderingInitializeNative(int width, int height);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingUpdateFrame")]
    private static extern void RenderingUpdateFrameNative();

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingShutdown")]
    private static extern void RenderingShutdownNative();

    public static void RenderingInitialize(int width, int height)
    {
        RenderingInitializeNative(width, height);
    }

    public static void RenderingUpdateFrame()
    {
        RenderingUpdateFrameNative();
    }

    public static void RenderingShutdown()
    {
        RenderingShutdownNative();
    }
}
