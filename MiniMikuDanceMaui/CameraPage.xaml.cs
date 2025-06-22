using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;

using Microsoft.Maui.Graphics;
using System.Threading.Tasks;

namespace MiniMikuDanceMaui;

public partial class CameraPage : ContentPage
{
    private bool _isFullscreen;
    private bool _sidebarOpen;
    private const double ModeItemWidth = 110;
    private const double HighlightThreshold = 55;
    private const double SidebarWidth = 340;
    private const double SidebarEdgeWidth = 12;
    private bool _panTracking;
    private int _centerIndex;
    private readonly string[] _modeTitles =
    {
        "IMPORT",
        "POSE",
        "MOTION",
        "AR",
        "RECORD"
    };

    public CameraPage()
    {
        InitializeComponent();
        this.SizeChanged += OnSizeChanged;
        FsToggleBtn.Clicked += OnFsToggle;
        FsToggleBtn.Pressed += (s, e) => FsToggleBtn.FadeTo(0.8, 100);
        FsToggleBtn.Released += (s, e) => FsToggleBtn.FadeTo(1, 100);
        var shutterTap = new TapGestureRecognizer { Command = new Command(async () => await FlashShutter()) };
        ShutterBtn.GestureRecognizers.Add(shutterTap);
        // mode labels
        foreach (var title in _modeTitles)
        {
            var label = new Label
            {
                Text = title.ToUpper(),
                WidthRequest = ModeItemWidth,
                FontSize = 16,
                FontFamily = "NotoSans",
                CharacterSpacing = 0.2,
                TextColor = Color.FromArgb("#8E8E93"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            ModeStack.Children.Add(label);
        }
        ModeCarousel.Scrolled += OnModeScrolled;

        var swipeLeft = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
        swipeLeft.Swiped += (s, e) => ScrollToMode(_centerIndex + 1);
        var swipeRight = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
        swipeRight.Swiped += (s, e) => ScrollToMode(_centerIndex - 1);
        ModeCarousel.GestureRecognizers.Add(swipeLeft);
        ModeCarousel.GestureRecognizers.Add(swipeRight);

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnRootPan;
        Root.GestureRecognizers.Add(pan);

        UpdateModeHighlight();
    }

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateLayout();

    private void OnModeScrolled(object? sender, ScrolledEventArgs e)
    {
        double center = e.ScrollX + ModeCarousel.Width / 2;
        int index = (int)Math.Round((center - ModeItemWidth / 2) / ModeItemWidth);
        index = Math.Clamp(index, 0, ModeStack.Children.Count - 1);
        if (index != _centerIndex)
        {
            _centerIndex = index;
        }
        UpdateModeHighlight();
    }

    private async void ScrollToMode(int index)
    {
        index = Math.Clamp(index, 0, ModeStack.Children.Count - 1);
        await ModeCarousel.ScrollToAsync(index * ModeItemWidth, 0, true);
    }

    private void UpdateModeHighlight()
    {
        double center = ModeCarousel.ScrollX + ModeCarousel.Width / 2;
        for (int i = 0; i < ModeStack.Children.Count; i++)
        {
            if (ModeStack.Children[i] is Label label)
            {
                double itemCenter = i * ModeItemWidth + ModeItemWidth / 2;
                bool isCenter = Math.Abs(itemCenter - center) < HighlightThreshold;
                if (isCenter)
                {
                    label.TextColor = Color.FromArgb("#FFD500");
                    label.FontSize = 20;
                    label.FontAttributes = FontAttributes.Bold;
                    label.Shadow = new Shadow
                    {
                        Offset = new Point(0, 1),
                        Radius = 2,
                        Brush = new SolidColorBrush(Color.FromArgb("#66000000"))
                    };
                }
                else
                {
                    label.TextColor = Color.FromArgb("#8E8E93");
                    label.FontSize = 16;
                    label.FontAttributes = FontAttributes.None;
                    label.Shadow = null;
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
        LowerPaneBody.Opacity = _isFullscreen ? 0 : 1;

        AbsoluteLayout.SetLayoutBounds(ShutterBtn, new Rect((W - 88) / 2, H - safe.Bottom - 88 - 92, 88, 88));
        AbsoluteLayout.SetLayoutFlags(ShutterBtn, AbsoluteLayoutFlags.None);

        double sidebarX = _sidebarOpen ? 0 : -SidebarWidth;
        AbsoluteLayout.SetLayoutBounds(Sidebar, new Rect(sidebarX, 0, SidebarWidth, H));
        AbsoluteLayout.SetLayoutFlags(Sidebar, AbsoluteLayoutFlags.None);
    }

    private async void OnFsToggle(object? sender, EventArgs e)
    {
        _isFullscreen = !_isFullscreen;
        double targetH = _isFullscreen ? this.Height : this.Height * 0.618;
        await Viewer.LayoutTo(new Rect(0, 0, this.Width, targetH), 250, Easing.SinOut);
        await Task.WhenAll(
            ModeCarousel.FadeTo(_isFullscreen ? 0 : 1, 150),
            LowerPaneBody.FadeTo(_isFullscreen ? 0 : 1, 150)
        );
        UpdateLayout();
    }

    private async Task FlashShutter()
    {
        if (ShutterInner == null)
            return;
        ShutterInner.Color = Color.FromArgb("#DDDDDD");
        await Task.Delay(60);
        ShutterInner.Color = Colors.White;
    }

    private async void OnRootPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panTracking = true;
                break;
            case GestureStatus.Running:
                if (_panTracking)
                {
                    double x = Math.Clamp(-SidebarWidth + e.TotalX, -SidebarWidth, 0);
                    AbsoluteLayout.SetLayoutBounds(Sidebar, new Rect(x, 0, SidebarWidth, Height));
                }
                break;
            case GestureStatus.Canceled:
            case GestureStatus.Completed:
                if (_panTracking)
                {
                    bool open = e.TotalX > SidebarWidth / 2;
                    await AnimateSidebar(open);
                }
                _panTracking = false;
                break;
        }
    }

    private async Task AnimateSidebar(bool open)
    {
        double dest = open ? 0 : -SidebarWidth;
        await Sidebar.LayoutTo(new Rect(dest, 0, SidebarWidth, Height), 280, Easing.SinOut);
        Viewer.InputTransparent = open;
        _sidebarOpen = open;
        UpdateLayout();
    }
}
