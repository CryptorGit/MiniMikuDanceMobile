using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using MiniMikuDance.Motion;
using MiniMikuDance.App;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;

namespace MiniMikuDanceMaui;

public partial class TimeLineView : ContentView
{
    private MotionPlayer? _player;
    private int _frameCount;
    private int _currentFrame;
    private const int FrameWidth = 20;
    private const int VisibleRange = 30;
    private MotionEditor? _editor;
    private readonly List<string> _bones = new();
    private readonly Dictionary<string, SKBitmap> _cache = new();
    private readonly Dictionary<string, (int Start, int End)> _cacheRange = new();
    private readonly Dictionary<string, SKCanvasView> _cursorLayers = new();
    private IList<string> _availableBones = new List<string>();
    private string? _editingBone;
    private int _editingFrame;

    public static readonly BindableProperty AddValidProperty =
        BindableProperty.Create(nameof(AddValid), typeof(bool), typeof(TimeLineView), false);

    public bool AddValid
    {
        get => (bool)GetValue(AddValidProperty);
        set => SetValue(AddValidProperty, value);
    }

    public static readonly BindableProperty EditValidProperty =
        BindableProperty.Create(nameof(EditValid), typeof(bool), typeof(TimeLineView), false);

    public bool EditValid
    {
        get => (bool)GetValue(EditValidProperty);
        set => SetValue(EditValidProperty, value);
    }

    public MotionEditor? MotionEditor
    {
        get => _editor;
        set
        {
            _editor = value;
            InitializeTimeline();
        }
    }

    public IList<string> AvailableBones
    {
        get => _availableBones;
        set
        {
            _availableBones = value;
            UpdateBonePickers();
        }
    }

    public MotionPlayer? MotionPlayer
    {
        get => _player;
        set
        {
            if (_player != null)
                _player.OnFramePlayed -= OnFramePlayed;
            _player = value;
            if (_player != null)
            {
                _player.OnFramePlayed += OnFramePlayed;
                _frameCount = _editor?.Motion.Frames.Length ?? App.Initializer.Motion?.Frames.Length ?? 0;
            }
            TimeSlider.InvalidateSurface();
        }
    }
    public TimeLineView()
    {
        InitializeComponent();
        MotionPlayer = App.Initializer.MotionPlayer;
    }

    private void InitializeTimeline()
    {
        TimeGrid.RowDefinitions.Clear();
        TimeGrid.Children.Clear();
        _bones.Clear();
        _cursorLayers.Clear();
        _cache.Clear();

        foreach (var bmp in _cache.Values)
            bmp.Dispose();
        _cache.Clear();

        if (_editor != null)
        {
            _frameCount = _editor.Motion.Frames.Length;
            foreach (var bone in _editor.Motion.KeyFrames.Keys)
            {
                AddBoneRow(bone);
            }
        }
        UpdateBonePickers();
        TimeSlider.InvalidateSurface();
    }

    private void UpdateBonePickers()
    {
        var remaining = new List<string>(_availableBones);
        foreach (var b in _bones)
            remaining.Remove(b);
        AddBonePicker.ItemsSource = remaining;
        EditBonePicker.ItemsSource = new List<string>(_bones);
        DeleteBonePicker.ItemsSource = new List<string>(_bones);
    }

    private void AddBoneRow(string bone)
    {
        int row = TimeGrid.RowDefinitions.Count;
        TimeGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new Label
        {
            Text = bone,
            TextColor = (Color)Application.Current.Resources["TextColor"],
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(4, 0)
        };
        TimeGrid.Add(label, 0, row);

        var rowGrid = new Grid();
        var canvas = new SKCanvasView
        {
            EnableTouchEvents = true,
            WidthRequest = _frameCount * FrameWidth,
            HeightRequest = 30
        };
        canvas.BindingContext = bone;
        canvas.PaintSurface += OnKeyCanvasPaintSurface;
        canvas.Touch += OnKeyCanvasTouched;
        rowGrid.Add(canvas);

        var cursor = new SKCanvasView
        {
            InputTransparent = true,
            WidthRequest = _frameCount * FrameWidth,
            HeightRequest = 30
        };
        cursor.BindingContext = bone;
        cursor.PaintSurface += OnCursorPaintSurface;
        rowGrid.Add(cursor);
        TimeGrid.Add(rowGrid, 1, row);

        _bones.Add(bone);
        _cursorLayers[bone] = cursor;
        BuildCache(bone, 0, _frameCount - 1);
    }

    private void BuildCache(string bone, int start, int end)
    {
        if (_editor == null || !_editor.Motion.KeyFrames.TryGetValue(bone, out var set))
            return;

        var width = (end - start + 1) * FrameWidth;
        var bmp = new SKBitmap(width, 30);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear();
        using var paint = new SKPaint { Color = SKColors.LightGreen };
        foreach (var f in set)
        {
            if (f < start || f > end) continue;
            float x = (f - start) * FrameWidth + FrameWidth / 2f;
            canvas.DrawCircle(x, bmp.Height / 2f, 5, paint);
        }
        if (_cache.TryGetValue(bone, out var oldBmp))
            oldBmp.Dispose();
        _cache[bone] = bmp;
        _cacheRange[bone] = (start, end);
    }

    private void InvalidateCursorLayers()
    {
        foreach (var cv in _cursorLayers.Values)
            cv.InvalidateSurface();
    }

    public void ShowAddOverlay()
    {
        UpdateBonePickers();
        CheckAddValid();
        Overlay.IsVisible = true;
        AddWindow.IsVisible = true;
        EditWindow.IsVisible = false;
        DeleteWindow.IsVisible = false;
    }

    public void ShowEditOverlay()
    {
        UpdateBonePickers();
        CheckEditValid();
        Overlay.IsVisible = true;
        AddWindow.IsVisible = false;
        EditWindow.IsVisible = true;
        DeleteWindow.IsVisible = false;
    }

    public void ShowDeleteOverlay()
    {
        UpdateBonePickers();
        OnDeleteBoneChanged(null, EventArgs.Empty);
        Overlay.IsVisible = true;
        AddWindow.IsVisible = false;
        EditWindow.IsVisible = false;
        DeleteWindow.IsVisible = true;
    }

    public void HideOverlay()
    {
        Overlay.IsVisible = false;
        AddWindow.IsVisible = false;
        EditWindow.IsVisible = false;
        DeleteWindow.IsVisible = false;
    }

    private void OnAddClicked(object? sender, EventArgs e) => ShowAddOverlay();
    private void OnEditClicked(object? sender, EventArgs e) => ShowEditOverlay();
    private void OnDeleteClicked(object? sender, EventArgs e) => ShowDeleteOverlay();
    private void OnCancelClicked(object? sender, EventArgs e) => HideOverlay();

    private void OnDeleteBoneChanged(object? sender, EventArgs e)
    {
        if (MotionEditor == null || DeleteBonePicker.SelectedItem is not string bone)
        {
            DeleteTimePicker.ItemsSource = null;
            return;
        }

        if (_editor!.Motion.KeyFrames.TryGetValue(bone, out var set))
            DeleteTimePicker.ItemsSource = set.Select(f => f).ToList();
        else
            DeleteTimePicker.ItemsSource = null;
    }

    private void OnAddInputChanged(object? sender, EventArgs e) => CheckAddValid();

    private void OnEditInputChanged(object? sender, EventArgs e) => CheckEditValid();

    private void CheckAddValid()
        => AddValid = AddBonePicker.SelectedItem is string && int.TryParse(AddTimeEntry.Text, out _);

    private void CheckEditValid()
        => EditValid = EditBonePicker.SelectedItem is string && int.TryParse(EditTimeEntry.Text, out _);

    private void OnAddConfirmClicked(object? sender, EventArgs e)
    {
        if (MotionEditor == null)
        {
            HideOverlay();
            return;
        }

        if (AddBonePicker.SelectedItem is string bone &&
            int.TryParse(AddTimeEntry.Text, out var frame))
        {
            _editor!.AddKeyFrame(bone, frame);
            if (!_bones.Contains(bone))
                AddBoneRow(bone);
            else
                BuildCache(bone, 0, _frameCount - 1);
            UpdateBonePickers();
            // レイアウトを更新するため、ビュー全体の測定を無効化する
            this.InvalidateMeasure();
        }
        HideOverlay();
    }

    private void OnEditConfirmClicked(object? sender, EventArgs e)
    {
        if (MotionEditor == null)
        {
            HideOverlay();
            return;
        }

        if (EditBonePicker.SelectedItem is string newBone && int.TryParse(EditTimeEntry.Text, out var newFrame))
        {
            var oldBone = _editingBone ?? newBone;
            var oldFrame = _editingFrame;

            if (!string.IsNullOrEmpty(oldBone))
                _editor!.RemoveKeyFrame(oldBone, oldFrame);

            _editor!.AddKeyFrame(newBone, newFrame);

            if (!_bones.Contains(newBone))
                AddBoneRow(newBone);
            else
                BuildCache(newBone, 0, _frameCount - 1);

            if (oldBone != newBone)
                BuildCache(oldBone, 0, _frameCount - 1);

            UpdateBonePickers();
            this.InvalidateMeasure();
            _editingBone = newBone;
            _editingFrame = newFrame;
        }

        HideOverlay();
    }

    private void OnDeleteConfirmClicked(object? sender, EventArgs e)
    {
        if (MotionEditor == null)
        {
            HideOverlay();
            return;
        }

        if (DeleteBonePicker.SelectedItem is string bone && DeleteTimePicker.SelectedItem is int frame)
        {
            _editor!.RemoveKeyFrame(bone, frame);
            BuildCache(bone, 0, _frameCount - 1);
            OnDeleteBoneChanged(null, EventArgs.Empty);
            UpdateBonePickers();
            this.InvalidateMeasure();
        }

        HideOverlay();
    }

    private void OnAddSeqChanged(object? sender, ValueChangedEventArgs e)
        => AddSeqEntry.Text = ((int)e.NewValue).ToString();

    private void OnEditSeqChanged(object? sender, ValueChangedEventArgs e)
        => EditSeqEntry.Text = ((int)e.NewValue).ToString();

    private void OnSliderPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        var width = e.Info.Width;
        var height = e.Info.Height;
        using var paint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 2 };
        canvas.DrawLine(0, height / 2f, width, height / 2f, paint);

        if (_frameCount > 0)
        {
            float x = width * _currentFrame / (float)_frameCount;
            paint.Color = SKColors.Red;
            canvas.DrawLine(x, 0, x, height, paint);
        }
    }

    private void OnCursorPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvasView = (SKCanvasView)sender!;
        if (canvasView.BindingContext is not string bone) return;

        var canvas = e.Surface.Canvas;
        canvas.Clear();

        if (_frameCount > 0)
        {
            float x = _currentFrame * FrameWidth + FrameWidth / 2f;
            using var paint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 };
            canvas.DrawLine(x, 0, x, e.Info.Height, paint);
        }
    }

    private void OnKeyCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (MotionEditor == null) return;

        var canvasView = (SKCanvasView)sender!;
        if (canvasView.BindingContext is not string bone) return;

        var canvas = e.Surface.Canvas;
        canvas.Clear();

        int start = Math.Max(0, _currentFrame - VisibleRange);
        int end = Math.Min(_frameCount - 1, _currentFrame + VisibleRange);

        if (!_cache.TryGetValue(bone, out var bmp) ||
            !_cacheRange.TryGetValue(bone, out var range) ||
            range.Start != start || range.End != end)
        {
            BuildCache(bone, start, end);
            _cache.TryGetValue(bone, out bmp);
        }

        if (bmp != null)
        {
            canvas.DrawBitmap(bmp, start * FrameWidth, 0);
        }
    }

    private void OnKeyCanvasTouched(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType != SKTouchAction.Released || MotionEditor == null)
            return;

        var canvasView = (SKCanvasView)sender!;
        if (canvasView.BindingContext is not string bone) return;

        int frame = Math.Clamp((int)(e.Location.X / FrameWidth), 0, _frameCount - 1);
        if (_editor!.HasKeyFrame(bone, frame))
        {
            _editingBone = bone;
            _editingFrame = frame;
            EditBonePicker.SelectedItem = bone;
            EditTimeEntry.Text = frame.ToString();
            ShowEditOverlay();
        }
        e.Handled = true;
    }

    private void OnSliderTouched(object? sender, SKTouchEventArgs e)
    {
        if (_player == null || _frameCount == 0)
            return;

        if (e.ActionType == SKTouchAction.Pressed || e.ActionType == SKTouchAction.Moved)
        {
            var width = ((SKCanvasView)sender).CanvasSize.Width;
            var ratio = Math.Clamp(e.Location.X / width, 0f, 1f);
            _currentFrame = (int)(ratio * _frameCount);
            _player.Seek(_currentFrame);
            ((SKCanvasView)sender).InvalidateSurface();
            InvalidateCursorLayers();
            e.Handled = true;
        }
    }

    private void OnFramePlayed(MiniMikuDance.PoseEstimation.JointData obj)
    {
        if (_player == null) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _currentFrame = _player.FrameIndex;
            TimeSlider.InvalidateSurface();
            InvalidateCursorLayers();
        });
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        if (_player == null)
            return;

        var motion = App.Initializer.Motion;
        if (motion == null)
            return;

        if (_player.IsPlaying)
            return;

        if (_player.FrameIndex >= motion.Frames.Length)
            _player.Restart();
        else if (_player.FrameIndex == 0)
            _player.Play(motion);
        else
            _player.Resume();
    }

    private void OnPauseClicked(object? sender, EventArgs e) => _player?.Pause();

    private void OnStopClicked(object? sender, EventArgs e)
    {
        if (_player == null)
            return;
        _player.Stop();
        _currentFrame = 0;
        TimeSlider.InvalidateSurface();
        InvalidateCursorLayers();
    }
}
