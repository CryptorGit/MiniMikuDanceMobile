using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
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
    private readonly SimpleCubeRenderer _renderer = new();
    private bool _glInitialized;
    private readonly Dictionary<long, SKPoint> _touchPoints = new();

    public CameraPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.SizeChanged += OnSizeChanged;

        MenuButton.Clicked += async (s, e) => await AnimateSidebar(!_sidebarOpen);
        var overlayTap = new TapGestureRecognizer();
        overlayTap.Tapped += async (s, e) => await AnimateSidebar(false);
        MenuOverlay.GestureRecognizers.Add(overlayTap);

        HomeBtn.Clicked += async (s, e) =>
        {
            await Navigation.PopToRootAsync();
            await AnimateSidebar(false);
        };

        SettingBtn.Clicked += async (s, e) =>
        {
            await Navigation.PushAsync(new SettingPage());
            await AnimateSidebar(false);
        };

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
        try
        {
            MmdFileSystem.Ensure("Models");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create model folder: {ex.Message}");
        }
        _glInitialized = false;
        Viewer?.InvalidateSurface();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _glInitialized = false;
    }

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateLayout();

    private void UpdateLayout()
    {
        double W = this.Width;
        double H = this.Height;
        Thickness safe = this.Padding;

        AbsoluteLayout.SetLayoutBounds(Viewer, new Rect(0, 0, W, H));
        AbsoluteLayout.SetLayoutFlags(Viewer, AbsoluteLayoutFlags.None);
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
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select VRM file"
            });

            if (result != null)
            {
                if (Path.GetExtension(result.FileName).ToLowerInvariant() != ".vrm")
                {
                    await DisplayAlert("Invalid File", "Please select a .vrm file.", "OK");
                    return;
                }

                var importer = new MiniMikuDance.Import.ModelImporter();
                var data = importer.ImportModel(result.FullPath);
                _renderer.LoadModel(data);
                Viewer?.InvalidateSurface();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
