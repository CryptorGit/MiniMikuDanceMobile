using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using MiniMikuDance.Import;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using OpenTK.Mathematics;

namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
    const int MaxFrame = 60;
    const int MaxRows = 17;
    const int VisibleColumns = 7;
    const float FrameWidth = 60f;
    const float RowHeight = 60f;
    const float LeftPanelWidth = 90f;
    const float BoneNameFontSize = 32f;
    const float HeaderFontSize = 24f;

    private ModelData? _model;
    private readonly List<string> _boneNames = new List<string>();
    private readonly Dictionary<string, List<int>> _keyframes = new Dictionary<string, List<int>>();

    private readonly SKFont _boneNameFont;
    private readonly SKFont _headerFont;
    private readonly float _density = (float)DeviceDisplay.Current.MainDisplayInfo.Density;
    private int _currentFrame = 0;
    private bool _isScrolling = false;
    private float _scrollX = 0;
    private float _scrollY = 0;
    private int _selectedKeyInputBoneIndex = 0;

    public event EventHandler? AddKeyClicked;
    public event EventHandler? EditKeyClicked;
    public event EventHandler? DeleteKeyClicked;

    public ModelData? Model
    {
        get => _model;
        set
        {
            _model = value;
            _boneNames.Clear();
            if (_model != null)
            {
                var requiredHumanoidBones = new List<string>
                {
                    "hips", "spine", "chest", "neck", "head",
                    "leftUpperArm", "leftLowerArm", "leftHand",
                    "rightUpperArm", "rightLowerArm", "rightHand",
                    "leftUpperLeg", "leftLowerLeg", "leftFoot",
                    "rightUpperLeg", "rightLowerLeg", "rightFoot"
                };

                _boneNames.AddRange(_model.HumanoidBoneList
                    .Select(b => b.Name)
                    .Where(name => requiredHumanoidBones.Contains(name))
                    .Take(MaxRows));

                _keyframes.Clear();
                foreach (var boneName in _boneNames)
                {
                    _keyframes[boneName] = new List<int>();
                }
            }
            else
            {
                _keyframes.Clear();
            }
            UpdateCanvasSizes();
            InvalidateAll();
        }
    }

    public IReadOnlyList<string> BoneNames => _boneNames;

    public int SelectedKeyInputBoneIndex
    {
        get => _selectedKeyInputBoneIndex;
        set
        {
            if (_selectedKeyInputBoneIndex != value)
            {
                _selectedKeyInputBoneIndex = value;
                InvalidateAll();
            }
        }
    }

    public int CurrentFrame
    {
        get => _currentFrame;
        set
        {
            if (_currentFrame != value)
            {
                _currentFrame = value;
                UpdateFrameShift();
                InvalidateAll();
            }
        }
    }

    public TimelineView()
    {
        InitializeComponent();
        _boneNameFont = new SKFont(SKTypeface.Default, BoneNameFontSize);
        _headerFont = new SKFont(SKTypeface.Default, HeaderFontSize);
        BindingContext = this;
    }

    private void OnTimelineViewLoaded(object? sender, EventArgs e)
    {
        TimelineContentScrollView.Scrolled += OnTimelineContentScrolled;
        BoneNameScrollView.Scrolled += OnBoneNameScrolled;
        UpdateCanvasSizes();
        InvalidateAll();

#if ANDROID
        if (BoneNameScrollView.Handler?.PlatformView is Android.Views.View boneName)
        {
            boneName.OverScrollMode = Android.Views.OverScrollMode.Never;
        }
        #if ANDROID
        if (TimelineContentScrollView.Handler?.PlatformView is Android.Views.View outer)
        {
            outer.OverScrollMode = Android.Views.OverScrollMode.Never;      // 既存
            // 内側の HorizontalScrollView も探して止める
            if (outer is Android.Views.ViewGroup vg &&
                vg.ChildCount > 0 &&
                vg.GetChildAt(0) is Android.Widget.HorizontalScrollView inner)
            {
                inner.OverScrollMode = Android.Views.OverScrollMode.Never;
            }
        }
#endif
#elif IOS
        if (BoneNameScrollView.Handler?.PlatformView is UIKit.UIScrollView boneName)
        {
            boneName.Bounces = false;
            boneName.AlwaysBounceVertical = false;
        }
        if (TimelineContentScrollView.Handler?.PlatformView is UIKit.UIScrollView outer)
        {
            outer.Bounces = false;
            foreach (var sub in outer.Subviews)
                if (sub is UIKit.UIScrollView inner)
                    inner.Bounces = false;
        }
#elif WINDOWS
        if (BoneNameScrollView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer boneName)
        {
            boneName.IsVerticalScrollInertiaEnabled = false;
        }
        if (TimelineContentScrollView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer timelineContent)
        {
            timelineContent.IsHorizontalScrollInertiaEnabled = false;
            timelineContent.IsVerticalScrollInertiaEnabled = false;
            if (timelineContent.Content is Microsoft.UI.Xaml.FrameworkElement content && content is Microsoft.UI.Xaml.Controls.ScrollViewer innerScrollViewer)
            {
                innerScrollViewer.IsHorizontalScrollInertiaEnabled = false;
            }
        }
#endif
    }

    private void OnTimelineContentScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isScrolling) return;
        _isScrolling = true;

        _scrollY = (float)e.ScrollY;
        BoneNameScrollView.ScrollToAsync(0, _scrollY, false);

        InvalidateAll();
        _isScrolling = false;
    }

    private void OnBoneNameScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isScrolling) return;
        _isScrolling = true;

        _scrollY = (float)e.ScrollY;
        TimelineContentScrollView.ScrollToAsync(0, _scrollY, false);

        InvalidateAll();
        _isScrolling = false;
    }

    public float TimelinePixelWidth { get; private set; }
    public float TimelinePixelHeight { get; private set; }
    private void UpdateCanvasSizes()
    {
        TimelinePixelWidth  = MaxFrame * FrameWidth;
        TimelinePixelHeight = _boneNames.Count * RowHeight;

        OnPropertyChanged(nameof(TimelinePixelWidth));
        OnPropertyChanged(nameof(TimelinePixelHeight));

        HeaderCanvas.WidthRequest    = TimelinePixelWidth;
        BoneNameCanvas.HeightRequest = TimelinePixelHeight;
        TimelineContentCanvas.WidthRequest = TimelinePixelWidth;
        TimelineContentCanvas.HeightRequest = TimelinePixelHeight;
    }

    private void InvalidateAll()
    {
        HeaderCanvas.InvalidateSurface();
        BoneNameCanvas.InvalidateSurface();
        TimelineContentCanvas.InvalidateSurface();
    }

    private void UpdateFrameShift()
    {
        if (_currentFrame >= VisibleColumns)
            _scrollX = (_currentFrame - VisibleColumns + 1) * FrameWidth;
        else
            _scrollX = 0;
    }

    void OnHeaderPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(SKColors.Transparent);

        using var minorPaint = new SKPaint { Color = SKColors.LightGray, StrokeWidth = 1 };
        using var majorPaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 2 };
        using var fivePaint  = new SKPaint { Color = SKColors.Green, StrokeWidth = 1 };
        using var numberPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };
        using var markerPaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 };

        canvas.Translate(-_scrollX, 0);
        canvas.Scale(_density);

        for (int f = 0; f < MaxFrame; f++)
        {
            float x = f * FrameWidth;
            if (f % 5 == 0)
                canvas.DrawLine(x, 0, x, height, fivePaint);
            canvas.DrawLine(x, 0, x, height, f % 10 == 0 ? majorPaint : minorPaint);
            if (f % 10 == 0)
            {
                var text = f.ToString();
                canvas.DrawText(text, x + 2, height - 2, _headerFont, numberPaint);
            }
        }

        float markerX = CurrentFrame * FrameWidth + FrameWidth / 2;
        canvas.DrawLine(markerX, 0, markerX, height, markerPaint);
    }

    void OnBoneNamePaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        var width = info.Width / _density;
        canvas.Clear(new SKColor(40, 40, 40));

        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var selectedRowPaint = new SKPaint { Color = new SKColor(80, 80, 80) };
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        canvas.Translate(0, -_scrollY);
        canvas.Scale(_density);

        for (int i = 0; i < _boneNames.Count; i++)
        {
            var y = i * RowHeight;
            if (i == _selectedKeyInputBoneIndex)
            {
                canvas.DrawRect(0, y, width, RowHeight, selectedRowPaint);
            }
            else if (i % 2 == 1)
            {
                canvas.DrawRect(0, y, width, RowHeight, altRowPaint);
            }

            canvas.DrawLine(0, y + RowHeight, width, y + RowHeight, linePaint);
            float textY = y + (RowHeight - _boneNameFont.Size) / 2 + _boneNameFont.Size;
            canvas.DrawText(_boneNames[i], 10, textY, _boneNameFont, textPaint);
        }
    }

    void OnTimelineContentPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(SKColors.Transparent);

        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var minorPaint = new SKPaint { Color = SKColors.LightGray, StrokeWidth = 1 };
        using var majorPaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 2 };
        using var fivePaint  = new SKPaint { Color = SKColors.Green, StrokeWidth = 1 };
        using var keyframePaint = new SKPaint { Color = SKColors.Yellow, Style = SKPaintStyle.Fill };
        using var playheadPaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 };

        canvas.Translate(-_scrollX, -_scrollY);
        canvas.Scale(_density);

        var totalWidth = MaxFrame * FrameWidth;
        for (int i = 0; i < _boneNames.Count; i++)
        {
            var y = i * RowHeight;
            if (i % 2 == 1)
                canvas.DrawRect(0, y, totalWidth, RowHeight, altRowPaint);
            canvas.DrawLine(0, y + RowHeight, totalWidth, y + RowHeight, linePaint);
        }

        for (int f = 0; f < MaxFrame; f++)
        {
            var x = f * FrameWidth;
            if (f % 5 == 0)
                canvas.DrawLine(x, 0, x, _boneNames.Count * RowHeight, fivePaint);
            canvas.DrawLine(x, 0, x, _boneNames.Count * RowHeight, f % 10 == 0 ? majorPaint : minorPaint);
        }

        foreach (var boneName in _boneNames)
        {
            if (!_keyframes.TryGetValue(boneName, out var frames))
                continue;
            var row = _boneNames.IndexOf(boneName);
            foreach (var frame in frames)
            {
                var x = frame * FrameWidth + FrameWidth / 2;
                var y = row * RowHeight + RowHeight / 2;
                using var diamondPath = new SKPath();
                diamondPath.MoveTo(x, y - 6);
                diamondPath.LineTo(x + 6, y);
                diamondPath.LineTo(x, y + 6);
                diamondPath.LineTo(x - 6, y);
                diamondPath.Close();
                canvas.DrawPath(diamondPath, keyframePaint);
            }
        }

        var playX = CurrentFrame * FrameWidth + FrameWidth / 2;
        canvas.DrawLine(playX, 0, playX, _boneNames.Count * RowHeight, playheadPaint);
    }

    public List<int> GetKeyframesForBone(string boneName) => _keyframes.TryGetValue(boneName, out var frames) ? frames : new List<int>();
    public void AddKeyframe(string boneName, int frame) { /* ... */ }
    public void RemoveKeyframe(string boneName, int frame) { /* ... */ }
    public bool HasKeyframe(string boneName, int frame) => _keyframes.ContainsKey(boneName) && _keyframes[boneName].Contains(frame);
    public void ClearKeyframes() { /* ... */ }
    public Vector3 GetBoneTranslationAtFrame(string boneName, int frame) => Vector3.Zero;
    public Vector3 GetBoneRotationAtFrame(string boneName, int frame) => Vector3.Zero;

    private void OnPlayClicked(object? sender, EventArgs e) { /* ... */ }
    private void OnPauseClicked(object? sender, EventArgs e) { /* ... */ }
    private void OnStopClicked(object? sender, EventArgs e) { CurrentFrame = 0; CurrentFrameEntry.Text = "0"; }
    private void OnFrameToStartClicked(object? sender, EventArgs e) { CurrentFrame = 0; CurrentFrameEntry.Text = "0"; }
    private void OnFrameToEndClicked(object? sender, EventArgs e) { CurrentFrame = MaxFrame - 1; CurrentFrameEntry.Text = CurrentFrame.ToString(); }
    private void OnFrameMinusOneClicked(object? sender, EventArgs e) { CurrentFrame = Math.Max(0, CurrentFrame - 1); CurrentFrameEntry.Text = CurrentFrame.ToString(); }
    private void OnFramePlusOneClicked(object? sender, EventArgs e) { CurrentFrame = Math.Min(MaxFrame - 1, CurrentFrame + 1); CurrentFrameEntry.Text = CurrentFrame.ToString(); }
    void OnAddKeyClicked(object? sender, EventArgs e) => AddKeyClicked?.Invoke(this, EventArgs.Empty);
    void OnEditKeyClicked(object? sender, EventArgs e) => EditKeyClicked?.Invoke(this, EventArgs.Empty);
    void OnDeleteKeyClicked(object? sender, EventArgs e) => DeleteKeyClicked?.Invoke(this, EventArgs.Empty);
}
