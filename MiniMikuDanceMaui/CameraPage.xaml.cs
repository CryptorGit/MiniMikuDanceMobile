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
using System.Diagnostics;
using System.Threading.Tasks;
using ShapePath = Microsoft.Maui.Controls.Shapes.Path;

namespace MiniMikuDanceMaui;

public partial class CameraPage : ContentPage
{
    private bool _sidebarOpen;
    private const double ModeItemWidth = 88;
    private const double HighlightThreshold = 44;
    private const double SidebarWidth = 340;
    private const double SidebarEdgeWidth = 12;
    private bool _panTracking;
    private int _centerIndex;
    private CancellationTokenSource? _scrollEndCts;
    private bool _snapping;
    private bool _fullScreen;
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
                HeightRequest = 48,
                FontSize = 16,
                FontFamily = "NotoSans",
                CharacterSpacing = 0.2,
                TextColor = Color.FromArgb("#8E8E93"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                LineHeight = 1
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

        var stickLeftTap = new TapGestureRecognizer();
        stickLeftTap.Tapped += (s, e) => ScrollToMode(_centerIndex - 1);
        StickLeftArea.GestureRecognizers.Add(stickLeftTap);

        var stickRightTap = new TapGestureRecognizer();
        stickRightTap.Tapped += (s, e) => ScrollToMode(_centerIndex + 1);
        StickRightArea.GestureRecognizers.Add(stickRightTap);

        var stickTopTap = new TapGestureRecognizer();
        stickTopTap.Tapped += async (s, e) => await ExitFullScreen();
        StickTopArea.GestureRecognizers.Add(stickTopTap);

        var stickBottomTap = new TapGestureRecognizer();
        stickBottomTap.Tapped += async (s, e) => await EnterFullScreen();
        StickBottomArea.GestureRecognizers.Add(stickBottomTap);

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnRootPan;
        Root.GestureRecognizers.Add(pan);

        UpdateModeHighlight();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ScrollToMode(_centerIndex);
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
        double bottomH = H - lowerY;
        AbsoluteLayout.SetLayoutBounds(LowerPaneBody, new Rect(0, lowerY, W, bottomH));
        AbsoluteLayout.SetLayoutFlags(LowerPaneBody, AbsoluteLayoutFlags.None);
        LowerPaneBody.Opacity = 1;
        double stickY = lowerY + (bottomH - 120) / 2;
        AbsoluteLayout.SetLayoutBounds(StickPad, new Rect((W - 120) / 2, stickY, 120, 120));
        AbsoluteLayout.SetLayoutFlags(StickPad, AbsoluteLayoutFlags.None);
        double sidebarX = _sidebarOpen ? 0 : -SidebarWidth;

        AbsoluteLayout.SetLayoutBounds(Sidebar, new Rect(sidebarX, 0, SidebarWidth, H));
        AbsoluteLayout.SetLayoutFlags(Sidebar, AbsoluteLayoutFlags.None);

        UpdateModeHighlight();
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

    private async Task EnterFullScreen()
    {
        if (_fullScreen)
            return;
        _fullScreen = true;
        double height = LowerPaneBody.Height;
        var tasks = new Task[]
        {
            ModeCarousel.TranslateTo(0, height, 200, Easing.SinOut),
            ModeTapOverlay.TranslateTo(0, height, 200, Easing.SinOut),
            ModeSeparator.TranslateTo(0, height, 200, Easing.SinOut),
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
            ModeCarousel.TranslateTo(0, 0, 200, Easing.SinOut),
            ModeTapOverlay.TranslateTo(0, 0, 200, Easing.SinOut),
            ModeSeparator.TranslateTo(0, 0, 200, Easing.SinOut),
            LowerPaneBody.TranslateTo(0, 0, 200, Easing.SinOut),
            StickPad.TranslateTo(0, 0, 200, Easing.SinOut)
        };
        await Task.WhenAll(tasks);
    }
}
