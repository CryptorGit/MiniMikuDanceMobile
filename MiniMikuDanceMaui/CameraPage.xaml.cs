#if ANDROID
using Android.Graphics.Drawables.Shapes;
#endif
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ShapePath = Microsoft.Maui.Controls.Shapes.Path;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using OpenTK.Graphics.ES30;
using Microsoft.Maui.Storage;
using System.IO;
using System.Linq;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class CameraPage : ContentPage
{
    private bool _sidebarOpen;
    private const double SidebarWidthRatio = 0.35; // 画面幅に対する割合
    private bool _fullScreen;
    private readonly SimpleCubeRenderer _renderer = new();
    private bool _glInitialized;
    private readonly Dictionary<long, SKPoint> _touchPoints = new();

    public CameraPage()
    {
        InitializeComponent();
        this.SizeChanged += OnSizeChanged;
        var shutterTap = new TapGestureRecognizer { Command = new Command(async () => await FlashShutter()) };
        ShutterBtn.GestureRecognizers.Add(shutterTap);
        var stickPan = new PanGestureRecognizer();
        stickPan.PanUpdated += OnStickPan;
        ShutterBtn.GestureRecognizers.Add(stickPan);
        ShutterInner.GestureRecognizers.Add(stickPan);



        var stickTopTap = new TapGestureRecognizer();
        stickTopTap.Tapped += async (s, e) => await ExitFullScreen();
        StickTopArea.GestureRecognizers.Add(stickTopTap);

        var stickBottomTap = new TapGestureRecognizer();
        stickBottomTap.Tapped += async (s, e) => await EnterFullScreen();
        StickBottomArea.GestureRecognizers.Add(stickBottomTap);

        MenuButton.Clicked += async (s, e) => await AnimateSidebar(!_sidebarOpen);
        var overlayTap = new TapGestureRecognizer();
        overlayTap.Tapped += async (s, e) => await AnimateSidebar(false);
        MenuOverlay.GestureRecognizers.Add(overlayTap);

        ImportBtn.Text = "SELECT";
        ImportBtn.Clicked += async (s, e) => await ShowModelSelector();

        ResetCamBtn.Clicked += (s, e) =>
        {
            _renderer.ResetCamera();
            Viewer?.InvalidateSurface();
        };

        if (Viewer is SKGLView glView)
        {
            glView.PaintSurface += OnPaintSurface;
            glView.Touch += OnViewTouch;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _glInitialized = false;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _renderer.Dispose();
        _glInitialized = false;
    }

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateLayout();

    private void UpdateLayout()
    {
        double W = this.Width;
        double H = this.Height;
        Thickness safe = this.Padding;

        // use 4:1 ratio for upper viewer and lower control pane
        double viewerH = H * 0.8;
        AbsoluteLayout.SetLayoutBounds(Viewer, new Rect(0, 0, W, viewerH));
        AbsoluteLayout.SetLayoutFlags(Viewer, AbsoluteLayoutFlags.None);

        double lowerY = viewerH;
        double bottomH = H - lowerY;
        AbsoluteLayout.SetLayoutBounds(LowerPaneBody, new Rect(0, lowerY, W, bottomH));
        AbsoluteLayout.SetLayoutFlags(LowerPaneBody, AbsoluteLayoutFlags.None);
        LowerPaneBody.Opacity = 1;
        double stickY = lowerY + (bottomH - 120) / 2;
        AbsoluteLayout.SetLayoutBounds(StickPad, new Rect((W - 120) / 2, stickY, 120, 120));
        AbsoluteLayout.SetLayoutFlags(StickPad, AbsoluteLayoutFlags.None);
        double menuWidth = W * SidebarWidthRatio;
        double sidebarX = _sidebarOpen ? W - menuWidth : W;
        AbsoluteLayout.SetLayoutBounds(Sidebar, new Rect(sidebarX, 0, menuWidth, H));
        AbsoluteLayout.SetLayoutFlags(Sidebar, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(MenuButton, new Rect(W - 72, H - 72, 56, 56));
        AbsoluteLayout.SetLayoutFlags(MenuButton, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(MenuOverlay, new Rect(0, 0, W, H));
        AbsoluteLayout.SetLayoutFlags(MenuOverlay, AbsoluteLayoutFlags.None);
        MenuOverlay.IsVisible = _sidebarOpen;
    }


    private async Task FlashShutter()
    {
        if (ShutterInner == null)
            return;
        ShutterInner.Color = Color.FromArgb("#DDDDDD");
        await Task.Delay(60);
        ShutterInner.Color = Colors.White;
    }

    private const double StickRadius = 30;
    private void OnStickPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                double dx = e.TotalX;
                double dy = e.TotalY;
                double r = Math.Sqrt(dx * dx + dy * dy);
                if (r > StickRadius)
                {
                    double scale = StickRadius / r;
                    dx *= scale;
                    dy *= scale;
                }
                ShutterInner.TranslationX = dx;
                ShutterInner.TranslationY = dy;
                break;
            case GestureStatus.Canceled:
            case GestureStatus.Completed:
                int dir = GetDirection(ShutterInner.TranslationX, ShutterInner.TranslationY);
                OnStickAction(dir);
                ShutterInner.TranslateTo(0, 0, 80, Easing.SinOut);
                break;
        }
    }

    private int GetDirection(double dx, double dy)
    {
        double r = Math.Sqrt(dx * dx + dy * dy);
        if (r < 10) return 8;
        double ang = Math.Atan2(-dy, dx);
        ang = (ang + Math.PI * 2) % (Math.PI * 2);
        int dir = (int)Math.Round(ang / (Math.PI / 4)) % 8;
        return dir;
    }

    private void OnStickAction(int dir)
    {
        System.Diagnostics.Debug.WriteLine($"Stick dir {dir}");
        Viewer?.InvalidateSurface();
    }


    private async Task AnimateSidebar(bool open)
    {
        double menuWidth = Width * SidebarWidthRatio;
        double dest = open ? Width - menuWidth : Width;
        MenuOverlay.IsVisible = open;
        await Sidebar.LayoutTo(new Rect(dest, 0, menuWidth, Height), 280, Easing.SinOut);
        if (Viewer is SKGLView glView)
            glView.EnableTouchEvents = !open;
        _sidebarOpen = open;
        UpdateLayout();
        Viewer?.InvalidateSurface();
    }

    private async Task EnterFullScreen()
    {
        if (_fullScreen)
            return;
        _fullScreen = true;
        double height = LowerPaneBody.Height;
        var tasks = new Task[]
        {
            LowerPaneBody.TranslateTo(0, height, 200, Easing.SinOut),
            StickPad.TranslateTo(0, height, 200, Easing.SinOut)
        };
        await Task.WhenAll(tasks);
    }

    private async Task ExitFullScreen()
    {
        if (!_fullScreen)
            return;
        _fullScreen = false;
        var tasks = new Task[]
        {
            LowerPaneBody.TranslateTo(0, 0, 200, Easing.SinOut),
            StickPad.TranslateTo(0, 0, 200, Easing.SinOut)
        };
        await Task.WhenAll(tasks);
    }

    private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
    {
        if (!_glInitialized)
        {
            GL.LoadBindings(new SKGLViewBindingsContext());
            _renderer.Initialize();
            _glInitialized = true;
        }
        _renderer.Resize(e.BackendRenderTarget.Width, e.BackendRenderTarget.Height);
        _renderer.Render();
        GL.Flush();
    }

    private void OnViewTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType == SKTouchAction.Pressed)
        {
            _touchPoints[e.Id] = e.Location;
        }
        else if (e.ActionType == SKTouchAction.Moved)
        {
            var prevPoints = new Dictionary<long, SKPoint>(_touchPoints);
            _touchPoints[e.Id] = e.Location;

            if (_touchPoints.Count == 1 && prevPoints.ContainsKey(e.Id))
            {
                var prev = prevPoints[e.Id];
                var dx = e.Location.X - prev.X;
                var dy = e.Location.Y - prev.Y;
                _renderer.Orbit(dx, dy);
            }
            else if (_touchPoints.Count == 2 && prevPoints.Count == 2)
            {
                var ids = new List<long>(_touchPoints.Keys);
                var p0Old = prevPoints[ids[0]];
                var p1Old = prevPoints[ids[1]];
                var p0New = _touchPoints[ids[0]];
                var p1New = _touchPoints[ids[1]];
                var oldMid = new SKPoint((p0Old.X + p1Old.X) / 2, (p0Old.Y + p1Old.Y) / 2);
                var newMid = new SKPoint((p0New.X + p1New.X) / 2, (p0New.Y + p1New.Y) / 2);
                _renderer.Pan(newMid.X - oldMid.X, newMid.Y - oldMid.Y);
                float oldDist = (p0Old - p1Old).Length;
                float newDist = (p0New - p1New).Length;
                _renderer.Dolly(oldDist - newDist);
            }
        }
        else if (e.ActionType == SKTouchAction.Released || e.ActionType == SKTouchAction.Cancelled)
        {
            _touchPoints.Remove(e.Id);
        }
        e.Handled = true;
        Viewer?.InvalidateSurface();
    }

    private async Task ShowModelSelector()
    {
        string folder = System.IO.Path.Combine(FileSystem.AppDataDirectory, "Models");
        System.IO.Directory.CreateDirectory(folder);
        var files = System.IO.Directory.GetFiles(folder, "*.vrm");
        if (files.Length == 0)
        {
            await DisplayAlert("No Models", $"Place VRM files in {folder}", "OK");
            return;
        }
        var names = System.Linq.Enumerable.Select(files, f => System.IO.Path.GetFileName(f));
        string? choice = await DisplayActionSheet("Select Model", "Cancel", null, names.ToArray());
        if (choice != null && choice != "Cancel")
        {
            var path = System.IO.Path.Combine(folder, choice);
            var importer = new MiniMikuDance.Import.ModelImporter();
            var data = importer.ImportModel(path);
            _renderer.LoadModel(data);
            Viewer?.InvalidateSurface();
        }
    }
}
