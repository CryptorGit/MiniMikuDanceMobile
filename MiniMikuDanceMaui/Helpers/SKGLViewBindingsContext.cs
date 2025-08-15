using OpenTK;
using System;

namespace MiniMikuDanceMaui.Helpers;

internal class SKGLViewBindingsContext : IBindingsContext
{
    public nint GetProcAddress(string procName)
    {
#if ANDROID
        return EglGetProcAddress(procName);
#elif IOS
        return Dlsym(RTLD_DEFAULT, procName);
#else
        return IntPtr.Zero;
#endif
    }

#if ANDROID
    [System.Runtime.InteropServices.DllImport("libEGL.so", EntryPoint = "eglGetProcAddress")]
    private static extern nint EglGetProcAddress(string procName);
#endif

#if IOS
    private static readonly nint RTLD_DEFAULT = 0;
    [System.Runtime.InteropServices.DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlsym")]
    private static extern nint Dlsym(nint handle, string symbol);
#endif
}
