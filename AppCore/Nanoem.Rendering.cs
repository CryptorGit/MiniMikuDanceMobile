using System.Runtime.InteropServices;

namespace MiniMikuDance;

internal static partial class Nanoem
{
    private const string NativeLibName = "nanoem";

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemRenderingInitialize(int width, int height);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void nanoemRenderingUpdateFrame();

    public static void RenderingInitialize(int width, int height)
    {
        nanoemRenderingInitialize(width, height);
    }

    public static void RenderingUpdateFrame()
    {
        nanoemRenderingUpdateFrame();
    }
}
