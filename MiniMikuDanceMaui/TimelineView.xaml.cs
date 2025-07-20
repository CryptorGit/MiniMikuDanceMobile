using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using MiniMikuDance.Import;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using OpenTK.Mathematics;
using System;
using System.Linq;

namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
    public const int MaxFrame = 60;
    const int MaxRows = 18;
    const int VisibleColumns = 14;
    const int PlayheadStopColumn = 7; // 7より後はグリッドをスクロールさせる
    public static double FrameWidth { get; private set; } = 10.0;
    public static double RowHeight { get; private set; } = 30.0;
    public const double LeftPanelWidth = 100.0;
    const float BoneNameFontSize = 14f;
    const float HeaderFontSize = 12f;

    private static readonly string[] StandardHumanoidBones = HumanoidBones.StandardOrder;

    private ModelData? _model;
    private readonly List<string> _boneNames = new List<string>();
    private readonly Dictionary<string, List<int>> _keyframes = new Dictionary<string, List<int>>();
    private readonly Dictionary<string, Dictionary<int, Vector3>> _translations = new();
    private readonly Dictionary<string, Dictionary<int, Vector3>> _rotations = new();

    private readonly SKFont _boneNameFont;
    private readonly SKFont _headerFont;
    private readonly float _density = (float)DeviceDisplay.Current.MainDisplayInfo.Density;
    private double _lastWidth = -1;
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
                _model.HumanoidBoneList = HumanoidBones.StandardOrder
                    .Where(n => _model.HumanoidBones.ContainsKey(n))
                    .Select(n => (n, _model.HumanoidBones[n]))
                    .ToList();

                _boneNames.Add("camera");

                foreach (var name in StandardHumanoidBones)
                {
                    if (_model.HumanoidBones.ContainsKey(name) && _boneNames.Count < MaxRows)
                        _boneNames.Add(name);
                }

                _keyframes.Clear();
                foreach (var boneName in _boneNames)
                {
                    _keyframes[boneName] = new List<int>();
                }
                // デフォルトでフレーム0に原点姿勢のキーを追加
                foreach (var boneName in _boneNames)
                {
                    AddKeyframe(boneName, 0, Vector3.Zero, Vector3.Zero);
                }
            }
            else
            {
                _keyframes.Clear();
            }
            BoneSelectPicker.ItemsSource = _boneNames;
            BoneSelectPicker.SelectedIndex = _boneNames.Count > 0
                ? Math.Min(_selectedKeyInputBoneIndex, _boneNames.Count - 1)
                : -1;
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
                if (BoneSelectPicker.SelectedIndex != value)
                    BoneSelectPicker.SelectedIndex = value;
                InvalidateAll();
            }
        }
    }

    public string SelectedBoneName =>
        SelectedKeyInputBoneIndex >= 0 && SelectedKeyInputBoneIndex < _boneNames.Count
            ? _boneNames[SelectedKeyInputBoneIndex]
            : string.Empty;

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
                CurrentFrameChanged?.Invoke(_currentFrame);
            }
        }
    }

    public event Action<int>? CurrentFrameChanged;

    public TimelineView()
    {
        InitializeComponent();
        _boneNameFont = new SKFont(SKTypeface.Default, BoneNameFontSize);
        _headerFont = new SKFont(SKTypeface.Default, HeaderFontSize);
        BindingContext = this;
        CurrentFramePicker.ItemsSource = Enumerable.Range(0, MaxFrame).ToList();
        CurrentFramePicker.SelectedItem = 0;
    }

    private void OnTimelineViewLoaded(object? sender, EventArgs e)
    {
        var displayWidth = DeviceDisplay.Current.MainDisplayInfo.Width / _density;
        FrameWidth = (displayWidth - LeftPanelWidth)/ VisibleColumns;

        TimelineContentScrollView.Scrolled += OnTimelineContentScrolled;
        BoneNameScrollView.Scrolled += OnBoneNameScrolled;
        UpdateCanvasSizes();
        InvalidateAll();

#if ANDROID
        if (BoneNameScrollView.Handler?.PlatformView is Android.Views.View boneName)
        {
            boneName.OverScrollMode = Android.Views.OverScrollMode.Never;
        }
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

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || Math.Abs(width - _lastWidth) < 0.1)
            return;

        _lastWidth = width;
        FrameWidth = (width - LeftPanelWidth) / VisibleColumns;
        UpdateCanvasSizes();
        InvalidateAll();
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

    public double TimelinePixelWidth { get; private set; }
    public double TimelinePixelHeight { get; private set; }
    private void UpdateCanvasSizes()
    {
        TimelinePixelWidth  = VisibleColumns * FrameWidth;
        // 行数に合わせて縦幅を決定する
        TimelinePixelHeight = _boneNames.Count * RowHeight;

        OnPropertyChanged(nameof(TimelinePixelWidth));
        OnPropertyChanged(nameof(TimelinePixelHeight));

        HeaderCanvas.WidthRequest    = TimelinePixelWidth;
        BoneNameCanvas.HeightRequest = TimelinePixelHeight;
        TimelineContentCanvas.WidthRequest = TimelinePixelWidth;
        TimelineContentCanvas.HeightRequest = TimelinePixelHeight;

        // ScrollViewのコンテンツサイズが変わった際に
        // レイアウトを強制更新してスクロール範囲を再計算させる
        BoneNameScrollView.ForceLayout();
        TimelineContentScrollView.ForceLayout();
    }

    private void InvalidateAll()
    {
        HeaderCanvas.InvalidateSurface();
        BoneNameCanvas.InvalidateSurface();
        TimelineContentCanvas.InvalidateSurface();
    }

    private void UpdateFrameShift()
    {
        int shiftFrame = Math.Max(0, _currentFrame - PlayheadStopColumn);
        shiftFrame = Math.Min(shiftFrame, MaxFrame - VisibleColumns);
        _scrollX = (float)(shiftFrame * FrameWidth);
    }

    void OnHeaderPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        var width  = info.Width / _density;
        var height = info.Height / _density;
        canvas.Clear(SKColors.Transparent);

        using var minorPaint = new SKPaint { Color = SKColors.White, StrokeWidth = 1 };
        using var fivePaint  = new SKPaint { Color = SKColors.Green, StrokeWidth = 1 };
        using var numberPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var markerPaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 };

        canvas.Translate(-_scrollX * _density, 0);
        canvas.Scale(_density);

        for (int f = 0; f < MaxFrame; f++)
        {
            float x = (float)(f * FrameWidth);
            var paint = f % 5 == 0 ? fivePaint : minorPaint;
            canvas.DrawLine(x, 0, x, height, paint);
            var text = f.ToString();
            canvas.DrawText(text, x + 2, height - 2, _headerFont, numberPaint);
        }

        int displayFrame = CurrentFrame;
        float markerX = (float)(displayFrame * FrameWidth + FrameWidth / 2);
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
        using var selectedRowPaint = new SKPaint { Color = new SKColor(25, 25, 100) };
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        canvas.Translate(0, -_scrollY * _density);
        canvas.Scale(_density);

        for (int i = 0; i < _boneNames.Count; i++)
        {
            float y = (float)(i * RowHeight);
            if (i == _selectedKeyInputBoneIndex)
            {
                canvas.DrawRect(0, y, width, (float)RowHeight, selectedRowPaint);
            }
            else if (i % 2 == 0)
            {
                canvas.DrawRect(0, y, width, (float)RowHeight, altRowPaint);
            }

            canvas.DrawLine(0, y + (float)RowHeight, width, y + (float)RowHeight, linePaint);
            float textY = y + ((float)RowHeight - _boneNameFont.Size) / 2 + _boneNameFont.Size;
            canvas.DrawText(_boneNames[i], 4, textY, _boneNameFont, textPaint);
        }
    }

    void OnTimelineContentPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(new SKColor(40, 40, 40));

        using var altRowPaint = new SKPaint { Color = new SKColor(50, 50, 50) };
        using var selectedRowPaint = new SKPaint { Color = new SKColor(25, 25, 100) };
        using var linePaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
        using var minorPaint = new SKPaint { Color = SKColors.White, StrokeWidth = 1 };
        using var fivePaint  = new SKPaint { Color = SKColors.Green, StrokeWidth = 1 };
        using var keyframePaint = new SKPaint { Color = SKColors.Yellow, Style = SKPaintStyle.Fill };
        using var playheadPaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 };

        canvas.Translate(-_scrollX * _density, -_scrollY * _density);
        canvas.Scale(_density);

        var totalWidth = (float)(MaxFrame * FrameWidth);
        for (int i = 0; i < _boneNames.Count; i++)
        {
            float y = (float)(i * RowHeight);
            if (i == _selectedKeyInputBoneIndex)
            {
                canvas.DrawRect(0, y, totalWidth, (float)RowHeight, selectedRowPaint);
            }
            else if (i % 2 == 0)
            {
                canvas.DrawRect(0, y, totalWidth, (float)RowHeight, altRowPaint);
            }
            canvas.DrawLine(0, y + (float)RowHeight, totalWidth, y + (float)RowHeight, linePaint);
        }

        for (int f = 0; f < MaxFrame; f++)
        {
            float x = (float)(f * FrameWidth);
            var paint = f % 5 == 0 ? fivePaint : minorPaint;
            canvas.DrawLine(x, 0, x, _boneNames.Count * (float)RowHeight, paint);
        }

        foreach (var boneName in _boneNames)
        {
            if (!_keyframes.TryGetValue(boneName, out var frames))
                continue;
            var row = _boneNames.IndexOf(boneName);
            foreach (var frame in frames)
            {
                float x = (float)(frame * FrameWidth + FrameWidth / 2);
                float y = (float)(row * RowHeight + RowHeight / 2);
                using var diamondPath = new SKPath();
                diamondPath.MoveTo(x, y - 6);
                diamondPath.LineTo(x + 6, y);
                diamondPath.LineTo(x, y + 6);
                diamondPath.LineTo(x - 6, y);
                diamondPath.Close();
                canvas.DrawPath(diamondPath, keyframePaint);
            }
        }

        int displayFrame = CurrentFrame;
        float playX = (float)(displayFrame * FrameWidth + FrameWidth / 2);
        canvas.DrawLine(playX, 0, playX, _boneNames.Count * (float)RowHeight, playheadPaint);
    }

    public List<int> GetKeyframesForBone(string boneName)
        => _keyframes.TryGetValue(boneName, out var frames) ? frames : new List<int>();

    public void AddKeyframe(string boneName, int frame, Vector3 translation, Vector3 rotation)
    {
        if (!_keyframes.TryGetValue(boneName, out var list))
        {
            list = new List<int>();
            _keyframes[boneName] = list;
        }
        if (!list.Contains(frame))
        {
            list.Add(frame);
            list.Sort();
        }

        if (!_translations.TryGetValue(boneName, out var tdict))
        {
            tdict = new Dictionary<int, Vector3>();
            _translations[boneName] = tdict;
        }
        tdict[frame] = translation;

        if (!_rotations.TryGetValue(boneName, out var rdict))
        {
            rdict = new Dictionary<int, Vector3>();
            _rotations[boneName] = rdict;
        }
        rdict[frame] = rotation;

        InvalidateAll();
    }

    public void RemoveKeyframe(string boneName, int frame)
    {
        if (_keyframes.TryGetValue(boneName, out var list) && list.Remove(frame))
        {
            if (_translations.TryGetValue(boneName, out var tdict))
                tdict.Remove(frame);
            if (_rotations.TryGetValue(boneName, out var rdict))
                rdict.Remove(frame);
            InvalidateAll();
        }
    }

    public bool HasKeyframe(string boneName, int frame)
        => _keyframes.ContainsKey(boneName) && _keyframes[boneName].Contains(frame);

    public void ClearKeyframes()
    {
        foreach (var key in _keyframes.Keys.ToList())
            _keyframes[key].Clear();
        _translations.Clear();
        _rotations.Clear();
        InvalidateAll();
    }
    public Vector3 GetBoneTranslationAtFrame(string boneName, int frame)
    {
        if (_translations.TryGetValue(boneName, out var tdict) && tdict.TryGetValue(frame, out var v))
            return v;
        return Vector3.Zero;
    }

    public Vector3 GetBoneRotationAtFrame(string boneName, int frame)
    {
        if (_rotations.TryGetValue(boneName, out var rdict) && rdict.TryGetValue(frame, out var v))
            return v;
        return Vector3.Zero;
    }

    public bool HasAnyKeyframe(string boneName)
        => _keyframes.TryGetValue(boneName, out var list) && list.Count > 0;

    public Vector3 GetNearestTranslation(string boneName, int frame)
    {
        if (!_keyframes.TryGetValue(boneName, out var list) || list.Count == 0)
            return Vector3.Zero;
        var nearest = list.OrderBy(f => Math.Abs(f - frame)).First();
        return GetBoneTranslationAtFrame(boneName, nearest);
    }

    public Vector3 GetNearestRotation(string boneName, int frame)
    {
        if (!_keyframes.TryGetValue(boneName, out var list) || list.Count == 0)
            return Vector3.Zero;
        var nearest = list.OrderBy(f => Math.Abs(f - frame)).First();
        return GetBoneRotationAtFrame(boneName, nearest);
    }

    private void OnPlayClicked(object? sender, EventArgs e) { /* ... */ }
    private void OnPauseClicked(object? sender, EventArgs e) { /* ... */ }
    private void OnStopClicked(object? sender, EventArgs e) { CurrentFrame = 0; CurrentFramePicker.SelectedItem = 0; }
    private void OnFrameToStartClicked(object? sender, EventArgs e) { CurrentFrame = 0; CurrentFramePicker.SelectedItem = 0; }
    private void OnFrameToEndClicked(object? sender, EventArgs e) { CurrentFrame = MaxFrame - 1; CurrentFramePicker.SelectedItem = CurrentFrame; }
    private void OnFrameMinusOneClicked(object? sender, EventArgs e) { CurrentFrame = Math.Max(0, CurrentFrame - 1); CurrentFramePicker.SelectedItem = CurrentFrame; }
    private void OnFramePlusOneClicked(object? sender, EventArgs e) { CurrentFrame = Math.Min(MaxFrame - 1, CurrentFrame + 1); CurrentFramePicker.SelectedItem = CurrentFrame; }

    private void OnCurrentFrameChanged(object? sender, EventArgs e)
    {
        if (CurrentFramePicker.SelectedItem is int f)
            CurrentFrame = f;
    }
    private void OnBoneSelectChanged(object? sender, EventArgs e)
    {
        if (BoneSelectPicker.SelectedIndex >= 0)
            SelectedKeyInputBoneIndex = BoneSelectPicker.SelectedIndex;
    }
    void OnAddKeyClicked(object? sender, EventArgs e) => AddKeyClicked?.Invoke(this, EventArgs.Empty);
    void OnEditKeyClicked(object? sender, EventArgs e) => EditKeyClicked?.Invoke(this, EventArgs.Empty);
    void OnDeleteKeyClicked(object? sender, EventArgs e) => DeleteKeyClicked?.Invoke(this, EventArgs.Empty);

    public void RefreshScrollViews()
    {
        BoneNameScrollView.RefreshLayout();
        TimelineContentScrollView.RefreshLayout();
    }
}
