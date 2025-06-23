using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace MiniMikuDanceMaui;

internal class SKGLViewBindingsContext : IBindingsContext
{
    public nint GetProcAddress(string procName)
    {
        return GLFW.GetProcAddress(procName);
    }
}
