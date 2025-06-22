using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MiniMikuDanceMaui;

public partial class CameraPage : ContentPage
{
    private bool _isFullscreen;
    private bool _sidebarOpen;
    private const double ModeItemWidth = 88;
    private int _centerIndex;

    public CameraPage()
    {
        InitializeComponent();
        this.SizeChanged += OnSizeChanged;
        FsToggleBtn.Clicked += OnFsToggle;
        ShutterBtn.Text = "";
        FsToggleBtn.Pressed += (s, e) => FsToggleBtn.FadeTo(0.8, 100);
        FsToggleBtn.Released += (s, e) => FsToggleBtn.FadeTo(1, 100);
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
        ModeCarousel.Scrolled += OnModeScrolled;
        UpdateModeHighlight();
    }

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateLayout();

    private void OnModeScrolled(object? sender, ScrolledEventArgs e)
    {
        int index = (int)Math.Round(e.ScrollX / ModeItemWidth);
        index = Math.Clamp(index, 0, ModeStack.Children.Count - 1);
        if (index != _centerIndex)
        {
            _centerIndex = index;
            UpdateModeHighlight();
        }
    }

    private void UpdateModeHighlight()
    {
        for (int i = 0; i < ModeStack.Children.Count; i++)
        {
            if (ModeStack.Children[i] is Label label)
            {
                if (i == _centerIndex)
                {
                    label.TextColor = Color.FromArgb("#FFD500");
                    label.FontSize = 20;
                }
                else
                {
                    label.TextColor = Colors.Gray;
                    label.FontSize = 16;
                }
            }
        }
    }

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
        AbsoluteLayout.SetLayoutBounds(ModeSeparator, new Rect(0, viewerH + 64, W, 1));
        AbsoluteLayout.SetLayoutFlags(ModeSeparator, AbsoluteLayoutFlags.None);

        double lowerY = viewerH + 64;
        AbsoluteLayout.SetLayoutBounds(LowerPaneBody, new Rect(0, lowerY, W, H - lowerY));
        AbsoluteLayout.SetLayoutFlags(LowerPaneBody, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(ShutterBtn, new Rect((W - 96) / 2, H - safe.Bottom - 96 - 92, 96, 96));
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
