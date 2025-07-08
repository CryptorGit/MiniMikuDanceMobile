using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using MiniMikuDance.Motion;
using MiniMikuDance.App;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace MiniMikuDanceMaui;

public partial class TimeLineView : ContentView
{
    private MotionPlayer? _player;
    private int _frameCount;
    private int _currentFrame;
    private const int FrameWidth = 20;
    private MotionEditor? _editor;
    private readonly List<string> _bones = new();
    private IList<string> _availableBones = new List<string>();

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
            TextColor = Colors.White,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(4, 0)
        };
        TimeGrid.Add(label, 0, row);

        var canvas = new SKCanvasView
        {
            EnableTouchEvents = true,
            WidthRequest = _frameCount * FrameWidth,
            HeightRequest = 30
        };
        canvas.PaintSurface += OnKeyCanvasPaintSurface;
        canvas.Touch += OnKeyCanvasTouched;
        TimeGrid.Add(canvas, 1, row);

        _bones.Add(bone);
    }

    public void ShowAddOverlay()
    {
        UpdateBonePickers();
        Overlay.IsVisible = true;
        AddWindow.IsVisible = true;
        EditWindow.IsVisible = false;
        DeleteWindow.IsVisible = false;
    }

    public void ShowEditOverlay()
    {
        UpdateBonePickers();
        Overlay.IsVisible = true;
        AddWindow.IsVisible = false;
        EditWindow.IsVisible = true;
        DeleteWindow.IsVisible = false;
    }

    public void ShowDeleteOverlay()
    {
        UpdateBonePickers();
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
            UpdateBonePickers();
            Scroll.InvalidateMeasure();
            TimeGrid.InvalidateMeasure();
        }
        HideOverlay();
    }

    private void OnEditConfirmClicked(object? sender, EventArgs e)
    {
        // TODO: 編集処理を実装する
        HideOverlay();
    }

    private void OnDeleteConfirmClicked(object? sender, EventArgs e)
    {
        // TODO: 削除処理を実装する
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

    private void OnKeyCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (MotionEditor == null) return;

        var canvasView = (SKCanvasView)sender!;
        int row = Grid.GetRow(canvasView);
        if (row < 0 || row >= _bones.Count) return;
        string bone = _bones[row];

        var canvas = e.Surface.Canvas;
        canvas.Clear();

        using var paint = new SKPaint { Color = SKColors.LightGreen };
        if (_editor!.Motion.KeyFrames.TryGetValue(bone, out var set))
        {
            foreach (var f in set)
            {
                float x = f * FrameWidth + FrameWidth / 2f;
                canvas.DrawCircle(x, e.Info.Height / 2f, 5, paint);
            }
        }
    }

    private void OnKeyCanvasTouched(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType != SKTouchAction.Released || MotionEditor == null)
            return;

        var canvasView = (SKCanvasView)sender!;
        int row = Grid.GetRow(canvasView);
        if (row < 0 || row >= _bones.Count) return;
        string bone = _bones[row];

        int frame = (int)(e.Location.X / FrameWidth);
        if (_editor!.HasKeyFrame(bone, frame))
        {
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
    }
}
