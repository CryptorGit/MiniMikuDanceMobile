using System;
using MiniMikuDance.UI;
using ViewerApp;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace MiniMikuDanceApp.UI;

public class UIRenderer : IDisposable
{
    private readonly ImGuiController _controller;

    public UIRenderer(GameWindow window)
    {
        _controller = new ImGuiController(window.Size.X, window.Size.Y);
        UIManager.Instance.RegisterTextureLoader(_controller.CreateTexture);
        window.UpdateFrame += args => _controller.Update(window, (float)args.Time);
        window.RenderFrame += _ =>
        {
            UIManager.Instance.Render();
            _controller.Render();
        };
        window.Resize += OnResize;
    }

    private void OnResize(ResizeEventArgs e)
    {
        _controller.WindowResized(e.Width, e.Height);
    }

    public void Dispose()
    {
        _controller.Dispose();
    }
}
