using System;
using System.Numerics;

namespace MiniMikuDance.App;

public interface IViewer : IDisposable
{
    Vector2 Size { get; }
    event Action<float>? FrameUpdated;
    byte[] CaptureFrame();
}

public interface IRenderer : IDisposable
{
    void Initialize();
    void Render();
    void Resize(int width, int height);
}
