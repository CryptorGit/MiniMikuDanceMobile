using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;

using Microsoft.Maui.Graphics;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace MiniMikuDanceMaui;

public partial class CameraPage : ContentPage
{
    private bool _sidebarOpen;
    private const double ModeItemWidth = 160;
    private const double HighlightThreshold = 80;
    private const double SidebarWidth = 340;
    private const double SidebarEdgeWidth = 12;
    private bool _panTracking;
    private int _centerIndex;
    private CancellationTokenSource? _scrollEndCts;
    private bool _snapping;
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
        var shutterTap = new TapGestureRecognizer { Command = new Command(async () => await FlashShutter()) };
        ShutterBtn.GestureRecognizers.Add(shutterTap);
        var stickPan = new PanGestureRecognizer();
        stickPan.PanUpdated += OnStickPan;
        ShutterBtn.GestureRecognizers.Add(stickPan);
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

        var tapLeft = new TapGestureRecognizer();
        tapLeft.Tapped += (s, e) => ScrollToMode(_centerIndex - 1);
        LeftTapArea.GestureRecognizers.Add(tapLeft);

        var tapRight = new TapGestureRecognizer();
        tapRight.Tapped += (s, e) => ScrollToMode(_centerIndex + 1);
        RightTapArea.GestureRecognizers.Add(tapRight);

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnRootPan;
        Root.GestureRecognizers.Add(pan);

        UpdateModeHighlight();
    }

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateLayout();

    private void OnModeScrolled(object? sender, ScrolledEventArgs e)
    {
        double pad = ModeStack.Padding.Left;
        double center = e.ScrollX + ModeCarousel.Width / 2;
        int index = (int)Math.Round((center - pad - ModeItemWidth / 2) / ModeItemWidth);
        index = Math.Clamp(index, 0, ModeStack.Children.Count - 1);
        if (index != _centerIndex)
        {
            _centerIndex = index;
        }

        if (!_snapping)
            ScheduleSnap();

        UpdateModeHighlight();
    }

    private void ScheduleSnap()
    {
        _scrollEndCts?.Cancel();
        var cts = new CancellationTokenSource();
        _scrollEndCts = cts;
        Task.Delay(80).ContinueWith(t =>
        {
            if (!cts.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() => ScrollToMode(_centerIndex));
            }
        });
    }

    private async void ScrollToMode(int index)
    {
        index = Math.Clamp(index, 0, ModeStack.Children.Count - 1);
        _snapping = true;
        await ModeCarousel.ScrollToAsync(index * ModeItemWidth, 0, true);
        _snapping = false;
    }


    private void UpdateModeHighlight()
    {
        double pad = ModeStack.Padding.Left;
        double center = ModeCarousel.ScrollX + ModeCarousel.Width / 2;
        for (int i = 0; i < ModeStack.Children.Count; i++)
        {
            if (ModeStack.Children[i] is Label label)
            {
                double itemCenter = pad + i * ModeItemWidth + ModeItemWidth / 2;
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

        double viewerH = H * 0.618;
        AbsoluteLayout.SetLayoutBounds(Viewer, new Rect(0, 0, W, viewerH));
        AbsoluteLayout.SetLayoutFlags(Viewer, AbsoluteLayoutFlags.None);

        AbsoluteLayout.SetLayoutBounds(ModeCarousel, new Rect(0, viewerH, W, 48));
        AbsoluteLayout.SetLayoutFlags(ModeCarousel, AbsoluteLayoutFlags.None);
        ModeCarousel.Opacity = 1;
        AbsoluteLayout.SetLayoutBounds(ModeTapOverlay, new Rect(0, viewerH, W, 48));
        AbsoluteLayout.SetLayoutFlags(ModeTapOverlay, AbsoluteLayoutFlags.None);
        ModeTapOverlay.Opacity = 1;
        double sidePad = Math.Max(0, (W - ModeItemWidth) / 2);
        ModeStack.Padding = new Thickness(sidePad, 0);
        AbsoluteLayout.SetLayoutBounds(ModeSeparator, new Rect(0, viewerH + 48, W, 1));
        AbsoluteLayout.SetLayoutFlags(ModeSeparator, AbsoluteLayoutFlags.None);
        double lowerY = viewerH + 48;
        AbsoluteLayout.SetLayoutBounds(LowerPaneBody, new Rect(0, lowerY, W, H - lowerY));
        AbsoluteLayout.SetLayoutFlags(LowerPaneBody, AbsoluteLayoutFlags.None);
        LowerPaneBody.Opacity = 1;
        AbsoluteLayout.SetLayoutBounds(StickPad, new Rect((W - 120) / 2, H - safe.Bottom - 120 - 92, 120, 120));
        AbsoluteLayout.SetLayoutFlags(StickPad, AbsoluteLayoutFlags.None);

        for (int i = 0; i < 8; i++)
        {
            if (StickPad.FindByName<BoxView>($"StickDir{i}") is BoxView box)
            {
                double ang = Math.PI / 180 * i * 45;
                double r = 50;
                double x = 60 + r * Math.Cos(ang) - 10;
                double y = 60 + r * Math.Sin(ang) - 10;
                AbsoluteLayout.SetLayoutBounds(box, new Rect(x, y, 20, 20));
                AbsoluteLayout.SetLayoutFlags(box, AbsoluteLayoutFlags.None);
            }
        }

        double sidebarX = _sidebarOpen ? 0 : -SidebarWidth;
        AbsoluteLayout.SetLayoutBounds(Sidebar, new Rect(sidebarX, 0, SidebarWidth, H));
        AbsoluteLayout.SetLayoutFlags(Sidebar, AbsoluteLayoutFlags.None);
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
                ShutterInner.TranslateTo(0, 0, 80, Easing.SinOut);
                break;
        }
    }

    private async void OnRootPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panTracking = e.StartPosition.X <= SidebarEdgeWidth;
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
