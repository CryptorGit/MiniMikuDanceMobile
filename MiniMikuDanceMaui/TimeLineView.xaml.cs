using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using MiniMikuDance.Motion;
using MiniMikuDance.App;

namespace MiniMikuDanceMaui;

public partial class TimeLineView : ContentView
{
    private MotionPlayer? _player;
    private int _frameCount;
    private int _currentFrame;

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
                _frameCount = App.Initializer.Motion?.Frames.Length ?? 0;
            }
            TimeSlider.InvalidateSurface();
        }
    }
    public TimeLineView()
    {
        InitializeComponent();
        MotionPlayer = App.Initializer.MotionPlayer;
    }

    public void ShowAddOverlay()
    {
        Overlay.IsVisible = true;
        AddWindow.IsVisible = true;
        EditWindow.IsVisible = false;
        DeleteWindow.IsVisible = false;
    }

    public void ShowEditOverlay()
    {
        Overlay.IsVisible = true;
        AddWindow.IsVisible = false;
        EditWindow.IsVisible = true;
        DeleteWindow.IsVisible = false;
    }

    public void ShowDeleteOverlay()
    {
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
        // TODO: 実際の追加処理を実装する
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
