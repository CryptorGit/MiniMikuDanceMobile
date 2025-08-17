using System;
using System.Numerics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MiniMikuDance.App;
using MiniMikuDance.IK;
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

    private PointF _lastRotatePoint;
    private PointF _lastPanPoint;
    private float _lastScale = 1f;

    public BGFXView()
    {
        Drawable = new RenderDrawable(this);

        StartInteraction += OnTouchStart;
        DragInteraction += OnTouchDrag;
        EndInteraction += OnTouchEnd;

        var rotatePan = new PanGestureRecognizer { TouchPoints = 1 };
        rotatePan.PanUpdated += OnRotatePanUpdated;
        GestureRecognizers.Add(rotatePan);

        var movePan = new PanGestureRecognizer { TouchPoints = 2 };
        movePan.PanUpdated += OnMovePanUpdated;
        GestureRecognizers.Add(movePan);

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;
        GestureRecognizers.Add(pinch);
    }

    public IRenderer? Renderer
    {
        get => (IRenderer?)GetValue(RendererProperty);
        set => SetValue(RendererProperty, value);
    }

    public Vector2 Size => _size;

    public event Action<float>? FrameUpdated;

    public unsafe byte[] CaptureFrame()
    {
        int width = (int)_size.X;
        int height = (int)_size.Y;
        using var frameBuffer = new FrameBuffer(width, height, TextureFormat.BGRA8);
        Bgfx.SetViewFrameBuffer(0, frameBuffer);
        Renderer?.Render();
        var texture = frameBuffer.GetTexture(0);
        byte[] data = new byte[width * height * 4];
        fixed (byte* ptr = data)
        {
            texture.Read((nint)ptr, 0);
        }
        Bgfx.Frame();
        Bgfx.SetViewFrameBuffer(0, FrameBuffer.Invalid);
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

    private void OnRotatePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (Renderer is PmxRenderer renderer)
        {
            if (e.StatusType == GestureStatus.Started)
            {
                _lastRotatePoint = new PointF((float)e.TotalX, (float)e.TotalY);
            }
            else if (e.StatusType == GestureStatus.Running)
            {
                float dx = (float)e.TotalX - _lastRotatePoint.X;
                float dy = (float)e.TotalY - _lastRotatePoint.Y;
                renderer.Orbit(dx, dy);
                _lastRotatePoint = new PointF((float)e.TotalX, (float)e.TotalY);
                Invalidate();
            }
        }
    }

    private void OnMovePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (Renderer is PmxRenderer renderer)
        {
            if (e.StatusType == GestureStatus.Started)
            {
                _lastPanPoint = new PointF((float)e.TotalX, (float)e.TotalY);
            }
            else if (e.StatusType == GestureStatus.Running)
            {
                float dx = (float)e.TotalX - _lastPanPoint.X;
                float dy = (float)e.TotalY - _lastPanPoint.Y;
                renderer.Pan(dx, dy);
                _lastPanPoint = new PointF((float)e.TotalX, (float)e.TotalY);
                Invalidate();
            }
        }
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (Renderer is PmxRenderer renderer)
        {
            if (e.Status == GestureStatus.Started)
            {
                _lastScale = 1f;
            }
            else if (e.Status == GestureStatus.Running)
            {
                float delta = (float)(e.Scale - _lastScale) * 100f;
                renderer.Dolly(delta);
                _lastScale = (float)e.Scale;
                Invalidate();
            }
        }
    }

    private void OnTouchStart(object? sender, TouchEventArgs e)
    {
        if (Renderer is PmxRenderer)
        {
            var p = e.Touches[0];
            IkManager.PickBone(p.X, p.Y);
            Invalidate();
        }
    }

    private void OnTouchDrag(object? sender, TouchEventArgs e)
    {
        if (Renderer is PmxRenderer renderer && IkManager.SelectedBoneIndex >= 0)
        {
            var p = e.Touches[0];
            var ray = renderer.ScreenPointToRay(p.X, p.Y);
            var pos = IkManager.IntersectDragPlane(ray);
            if (pos.HasValue)
            {
                IkManager.UpdateTarget(IkManager.SelectedBoneIndex, pos.Value);
            }
        }
        Invalidate();
    }

    private void OnTouchEnd(object? sender, TouchEventArgs e)
    {
        IkManager.ReleaseSelection();
        Invalidate();
    }

    public void Dispose()
    {
        Drawable = null;
        StartInteraction -= OnTouchStart;
        DragInteraction -= OnTouchDrag;
        EndInteraction -= OnTouchEnd;
        GestureRecognizers.Clear();
        FrameUpdated = null;
        Renderer?.Dispose();
        Renderer = null;
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
