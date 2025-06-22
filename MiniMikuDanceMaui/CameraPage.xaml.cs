using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;

using Microsoft.Maui.Graphics;

namespace MiniMikuDanceMaui;

public partial class CameraPage : ContentPage
{
    private bool _isFullscreen;
    private bool _sidebarOpen;

    public CameraPage()
    {
        InitializeComponent();
        this.SizeChanged += OnSizeChanged;
        FsToggleBtn.Clicked += OnFsToggle;
        ShutterBtn.Text = "";
        // sample modes
        foreach (var title in new[] { "Pose", "AR", "Video" })
        {
            var label = new Label
            {
                Text = title,
                WidthRequest = 88,
                FontSize = 16,
                TextColor = Colors.Gray,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            ModeStack.Children.Add(label);
        }
    }

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateLayout();

    private void UpdateLayout()
    {
        double W = this.Width;
        double H = this.Height;
        Thickness safe = this.Padding;

        double viewerH = _isFullscreen ? H : H * 0.618;
        AbsoluteLayout.SetLayoutBounds(Viewer, new Rect(0, 0, W, viewerH));
        AbsoluteLayout.SetLayoutFlags(Viewer, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(FsToggleBtn, new Rect(W - 72, viewerH - 72, 56, 56));
        AbsoluteLayout.SetLayoutFlags(FsToggleBtn, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(ModeCarousel, new Rect(0, viewerH, W, 64));
        AbsoluteLayout.SetLayoutFlags(ModeCarousel, AbsoluteLayoutFlags.None);
        ModeCarousel.Opacity = _isFullscreen ? 0 : 1;

        double lowerY = viewerH + 64;
        AbsoluteLayout.SetLayoutBounds(LowerPaneBody, new Rect(0, lowerY, W, H - lowerY));
        AbsoluteLayout.SetLayoutFlags(LowerPaneBody, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(ShutterBtn, new Rect((W - 96) / 2, H - safe.Bottom - 96 - 80, 96, 96));
        AbsoluteLayout.SetLayoutFlags(ShutterBtn, AbsoluteLayoutFlags.None);

        double sidebarX = _sidebarOpen ? 0 : -340;
        AbsoluteLayout.SetLayoutBounds(Sidebar, new Rect(sidebarX, 0, 340, H));
        AbsoluteLayout.SetLayoutFlags(Sidebar, AbsoluteLayoutFlags.None);
    }

    private async void OnFsToggle(object? sender, EventArgs e)
    {
        _isFullscreen = !_isFullscreen;
        double targetH = _isFullscreen ? this.Height : this.Height * 0.618;
        await Viewer.LayoutTo(new Rect(0, 0, this.Width, targetH), 250, Easing.SinOut);
        await ModeCarousel.FadeTo(_isFullscreen ? 0 : 1, 150);
        UpdateLayout();
    }
}
