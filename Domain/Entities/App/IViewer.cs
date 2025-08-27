using System;
using OpenTK.Mathematics;

namespace MiniMikuDance.App;

public interface IViewer : IDisposable
{
    Vector2i Size { get; }
    event Action<float>? FrameUpdated;
    byte[] CaptureFrame();
}
