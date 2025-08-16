using System;
using System.Numerics;
using System.IO;
using System.Threading;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MiniMikuDance.App;
using SharpBgfx;

namespace MiniMikuDanceMaui;

public class BGFXView : GraphicsView, IViewer
{
    public static readonly BindableProperty RendererProperty =
        BindableProperty.Create(
            nameof(Renderer),
            typeof(IRenderer),
            typeof(BGFXView),
            propertyChanged: OnRendererChanged);

    private DateTime _lastFrameTime = DateTime.Now;
    private Vector2 _size;

    public BGFXView()
    {
        Drawable = new RenderDrawable(this);
    }

    public IRenderer? Renderer
    {
        get => (IRenderer?)GetValue(RendererProperty);
        set => SetValue(RendererProperty, value);
    }

    public Vector2 Size => _size;

    public event Action<float>? FrameUpdated;

    public byte[] CaptureFrame()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bgfx_{Guid.NewGuid():N}.png");
        Bgfx.RequestScreenShot(path);
        Bgfx.Frame();
        var timeout = DateTime.UtcNow.AddSeconds(1);
        while (!File.Exists(path) && DateTime.UtcNow < timeout)
        {
            Thread.Sleep(1);
        }
        byte[] data = Array.Empty<byte>();
        if (File.Exists(path))
        {
            data = File.ReadAllBytes(path);
            File.Delete(path);
        }
        return data;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        _size = new Vector2((float)width, (float)height);
        Renderer?.Resize((int)width, (int)height);
    }

    private static void OnRendererChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (newValue is IRenderer renderer)
        {
            renderer.Initialize();
        }
    }

    private sealed class RenderDrawable : IDrawable
    {
        private readonly BGFXView _view;
        public RenderDrawable(BGFXView view) => _view = view;
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var now = DateTime.Now;
            _view.Renderer?.Render();
            _view.FrameUpdated?.Invoke((float)(now - _view._lastFrameTime).TotalSeconds);
            _view._lastFrameTime = now;
        }
    }
}
