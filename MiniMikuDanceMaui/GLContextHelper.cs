using System;
using System.Runtime.InteropServices;

namespace MiniMikuDanceMaui;

internal static class GLContextHelper
{
#if ANDROID
    [DllImport("libEGL.so", EntryPoint = "eglGetCurrentContext")]
    private static extern IntPtr eglGetCurrentContext();
#endif

    public static bool HasCurrentContext()
    {
#if ANDROID
        return eglGetCurrentContext() != IntPtr.Zero;
#else
        // Assume context is valid on other platforms
        return true;
#endif
    }
}
