using System.Numerics;
using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static partial class Nanoem
{
    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingInitialize")]
    private static extern void RenderingInitializeNative(int width, int height);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingUpdateFrame")]
    private static extern void RenderingUpdateFrameNative();

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingRenderFrame")]
    private static extern void RenderingRenderFrameNative();

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingShutdown")]
    private static extern void RenderingShutdownNative();

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingSetCamera")]
    private static extern void RenderingSetCameraNative(in Vector3 position, in Vector3 target);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingSetLight")]
    private static extern void RenderingSetLightNative(in Vector3 direction);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingSetGridVisible")]
    private static extern void RenderingSetGridVisibleNative(int value);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nanoemRenderingSetStageSize")]
    private static extern void RenderingSetStageSizeNative(float value);

    public static void RenderingInitialize(int width, int height)
    {
        RenderingInitializeNative(width, height);
    }

    public static void RenderingUpdateFrame()
    {
        RenderingUpdateFrameNative();
    }

    public static void RenderingRenderFrame()
    {
        RenderingRenderFrameNative();
    }

    public static void RenderingSetCamera(Vector3 position, Vector3 target)
    {
        RenderingSetCameraNative(position, target);
    }

    public static void RenderingSetLight(Vector3 direction)
    {
        RenderingSetLightNative(direction);
    }

    public static void RenderingSetGridVisible(bool value)
    {
        RenderingSetGridVisibleNative(value ? 1 : 0);
    }

    public static void RenderingSetStageSize(float value)
    {
        RenderingSetStageSizeNative(value);
    }

    public static void RenderingShutdown()
    {
        RenderingShutdownNative();
    }
}
