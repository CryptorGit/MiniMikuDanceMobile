using System;
using MiniMikuDance.UI;
using ViewerApp;

namespace MiniMikuDanceApp.UI;

public class UIRenderer : IDisposable
{
    private readonly ImGuiController _controller;
    private readonly Viewer _viewer;

    public UIRenderer(Viewer viewer)
    {
        _viewer = viewer;
        _controller = new ImGuiController(viewer.Size.X, viewer.Size.Y);
        viewer.UIFrameUpdated += dt => _controller.Update(viewer, dt);
        viewer.RenderUI += () =>
        {
            UIManager.Instance.Render();
            _controller.Render();
        };
        viewer.Resize += e => _controller.WindowResized(viewer.Size.X, viewer.Size.Y);
    }

    public void Dispose()
    {
        _controller.Dispose();
    }
}
