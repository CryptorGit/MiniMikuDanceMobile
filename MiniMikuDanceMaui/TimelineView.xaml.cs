using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;
using MauiIcons.Core;
using MauiIcons.Material.Outlined;
using MiniMikuDance.Motion;

namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
    public event Action? PlayRequested;
    public event Action<int>? FrameChanged;
    public event Action? AddKeyRequested;

    public MotionEditor? Editor { get; set; }

    private bool _isPlaying;
    private const int DefaultFrameColumns = 20;
    private int _frameCount = DefaultFrameColumns;
    private const int RowHeight = 24;
    public const double RowSpacing = 4.0;
    private int _currentFrame;
    private bool _suppressScroll;
    private bool _initialized;
    private double _lastScrollX;
    private double _lastScrollY;
    private readonly TimelineDrawable _drawable = new();
    private readonly TimelineHeaderDrawable _headerDrawable = new();

    public TimelineView()
    {
        InitializeComponent();
        // URL スタイルの名前空間利用時に必要な一時的ワークアラウンド
        _ = new MauiIcon();
        BoneList.Spacing = RowSpacing;
        TimelineCanvas.Drawable = _drawable;
        FrameHeaderCanvas.Drawable = _headerDrawable;
    }

    public void SetFrameCount(int count)
    {
        _frameCount = Math.Max(1, count);
        TimelineSlider.Maximum = _frameCount > 0 ? _frameCount - 1 : 0;

        BuildTimeline();
        TimelineCanvas.Invalidate();
    }

    public void SetFrameIndex(int index)
    {
        _currentFrame = index;
        if ((int)TimelineSlider.Value != index)
            TimelineSlider.Value = index;
        _drawable.CurrentFrame = index;
        UpdateCurrentIndicator();
        TimelineCanvas.Invalidate();
    }

    public void UpdatePlayState(bool playing)
    {
        _isPlaying = playing;
        PlayButton.Source = (playing ? MaterialOutlinedIcons.Pause : MaterialOutlinedIcons.PlayArrow)
            .ToImageSource(Colors.White, 36);
    }


    public void SetBones(IEnumerable<string> bones)
    {
        BoneList.Children.Clear();
        _drawable.SetBones(bones);
        foreach (var name in _drawable.Bones)
        {
            BoneList.Children.Add(new Label
            {
                Text = name,
                TextColor = Colors.White,
                FontSize = 12,
                HeightRequest = RowHeight,
                LineBreakMode = LineBreakMode.NoWrap,
                MaxLines = 1,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                VerticalTextAlignment = TextAlignment.Center
            });
        }

        SetFrameCount(_frameCount);
        TimelineCanvas.Invalidate();
    }

    public void AddKeyFrame(string bone, int frame)
    {
        _drawable.AddKeyFrame(bone, frame);
        TimelineCanvas.Invalidate();
        SetFrameIndex(frame);
    }

    public void RemoveKeyFrame(string bone, int frame)
    {
        _drawable.RemoveKeyFrame(bone, frame);
        TimelineCanvas.Invalidate();
    }

    private void BuildTimeline()
    {
        if (!_initialized)
        {
            _initialized = true;
        }

        _drawable.SetFrameCount(_frameCount);
        _headerDrawable.SetFrameCount(_frameCount);

        TimelineCanvas.WidthRequest = 20 * _frameCount;
        TimelineCanvas.HeightRequest = _drawable.BoneCount * RowHeight + (_drawable.BoneCount - 1) * RowSpacing;
        FrameHeaderCanvas.WidthRequest = 20 * _frameCount;

        UpdateCurrentIndicator();
        FrameHeaderCanvas.Invalidate();
    }

    private void UpdateCurrentIndicator()
    {
        if (!_initialized)
            return;
        _headerDrawable.CurrentFrame = _currentFrame;
        FrameHeaderCanvas.Invalidate();
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        PlayRequested?.Invoke();
    }

    private void OnAddKeyClicked(object? sender, EventArgs e)
    {
        AddKeyRequested?.Invoke();
    }

    private void OnSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        _currentFrame = (int)e.NewValue;
        FrameChanged?.Invoke(_currentFrame);
        UpdateCurrentIndicator();
        TimelineCanvas.Invalidate();
    }

    private async void OnBoneListScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_suppressScroll)
            return;

        if (Math.Abs(e.ScrollY - _lastScrollY) < 0.5)
            return;

        _suppressScroll = true;
        await TimelineScrollView.ScrollToAsync(TimelineScrollView.ScrollX, e.ScrollY, false);
        _suppressScroll = false;

        _lastScrollY = e.ScrollY;
        _drawable.ScrollY = (float)e.ScrollY;
        _headerDrawable.ScrollX = (float)TimelineScrollView.ScrollX;
        FrameHeaderCanvas.Invalidate();
        TimelineCanvas.Invalidate();
    }

    private async void OnTimelineScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_suppressScroll)
            return;

        if (Math.Abs(e.ScrollX - _lastScrollX) < 0.5 && Math.Abs(e.ScrollY - _lastScrollY) < 0.5)
            return;

        _suppressScroll = true;
        await BoneListScrollView.ScrollToAsync(0, e.ScrollY, false);
        _suppressScroll = false;

        _lastScrollX = e.ScrollX;
        _lastScrollY = e.ScrollY;
        _drawable.ScrollX = (float)e.ScrollX;
        _drawable.ScrollY = (float)e.ScrollY;
        _headerDrawable.ScrollX = (float)e.ScrollX;
        FrameHeaderCanvas.Invalidate();
        TimelineCanvas.Invalidate();
    }

    private void OnTimelineTapped(object? sender, TappedEventArgs e)
    {
        var point = e.GetPosition(TimelineCanvas);
        if (point == null)
            return;

        int col = (int)((point.Value.X + _drawable.ScrollX) / 20f);
        int row = (int)((point.Value.Y + _drawable.ScrollY) / (RowHeight + RowSpacing));
        if (row < 0 || row >= _drawable.BoneCount)
            return;
        if (col < 0 || col >= _frameCount)
            return;

        if (_drawable.HasKeyFrame(row, col))
        {
            _drawable.RemoveKeyFrame(row, col);
            Editor?.RemoveKeyFrame(row >= 0 && row < _drawable.Bones.Count ? _drawable.Bones[row] : string.Empty, col);
        }
        else
        {
            _drawable.AddKeyFrame(row, col);
            Editor?.AddKeyFrame(row >= 0 && row < _drawable.Bones.Count ? _drawable.Bones[row] : string.Empty, col);
        }
        SetFrameIndex(col);
    }
}

internal class TimelineDrawable : IDrawable
{
    private int _frameCount = 20;
    private readonly List<HashSet<int>> _keyFrames = new();
    private readonly List<string> _bones = new();

    public float ScrollX { get; set; }
    public float ScrollY { get; set; }
    public int CurrentFrame { get; set; }
    public int BoneCount => _bones.Count;

    public IReadOnlyList<string> Bones => _bones;

    public void SetFrameCount(int count) => _frameCount = Math.Max(1, count);

    public void SetBones(IEnumerable<string> bones)
    {
        _bones.Clear();
        _bones.AddRange(bones);
        _keyFrames.Clear();
        foreach (var _ in _bones)
            _keyFrames.Add(new HashSet<int>());
    }

    public RectF AddKeyFrame(string bone, int frame)
    {
        int index = _bones.IndexOf(bone);
        if (index >= 0)
        {
            if (_keyFrames[index].Add(frame))
            {
                const float cellWidth = 20f;
                const float rowHeight = 24f;
                const float rowSpacing = (float)TimelineView.RowSpacing;
                float x = frame * cellWidth;
                float y = index * (rowHeight + rowSpacing);
                return new RectF(x, y, cellWidth, rowHeight);
            }
        }
        return RectF.Zero;
    }

    public RectF RemoveKeyFrame(string bone, int frame)
    {
        int index = _bones.IndexOf(bone);
        if (index >= 0 && _keyFrames[index].Remove(frame))
        {
            const float cellWidth = 20f;
            const float rowHeight = 24f;
            const float rowSpacing = (float)TimelineView.RowSpacing;
            float x = frame * cellWidth;
            float y = index * (rowHeight + rowSpacing);
            return new RectF(x, y, cellWidth, rowHeight);
        }
        return RectF.Zero;
    }

    public void AddKeyFrame(int row, int frame)
    {
        if (row >= 0 && row < _keyFrames.Count)
            _keyFrames[row].Add(frame);
    }

    public void RemoveKeyFrame(int row, int frame)
    {
        if (row >= 0 && row < _keyFrames.Count)
            _keyFrames[row].Remove(frame);
    }

    public bool HasKeyFrame(int row, int frame)
        => row >= 0 && row < _keyFrames.Count && _keyFrames[row].Contains(frame);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float cellWidth = 20f;
        const float rowHeight = 24f;
        const float rowSpacing = (float)TimelineView.RowSpacing;

        canvas.Translate(-ScrollX, -ScrollY);

        int startRow = Math.Max(0, (int)(ScrollY / (rowHeight + rowSpacing)));
        int endRow = Math.Min(_bones.Count - 1,
            (int)((ScrollY + dirtyRect.Height) / (rowHeight + rowSpacing)) + 1);
        int startCol = Math.Max(0, (int)(ScrollX / cellWidth));
        int endCol = Math.Min(_frameCount - 1,
            (int)((ScrollX + dirtyRect.Width) / cellWidth) + 1);

        for (int r = startRow; r <= endRow; r++)
        {
            float y = r * (rowHeight + rowSpacing);
            for (int c = startCol; c <= endCol; c++)
            {
                float x = c * cellWidth;
                canvas.FillColor = (r % 2 == 0) ? Color.FromArgb("#303030") : Color.FromArgb("#202020");
                canvas.FillRectangle(x, y, cellWidth, rowHeight);
            }

            var keys = _keyFrames[r].OrderBy(i => i).ToList();
            foreach (var k in keys)
            {
                if (k < startCol || k > endCol)
                    continue;
                float x = k * cellWidth;
                canvas.FillColor = Colors.White;
                var cx = x + cellWidth / 2f;
                var cy = y + rowHeight / 2f;
                var path = new PathF();
                path.MoveTo(cx, cy - 4);
                path.LineTo(cx + 4, cy);
                path.LineTo(cx, cy + 4);
                path.LineTo(cx - 4, cy);
                path.Close();
                canvas.FillPath(path);
            }

            for (int i = 0; i < keys.Count - 1; i++)
            {
                float x1 = keys[i] * cellWidth + cellWidth / 2f;
                float x2 = keys[i + 1] * cellWidth + cellWidth / 2f;
                float yLine = y + rowHeight / 2f;
                canvas.StrokeColor = Colors.Orange;
                canvas.StrokeSize = 3f;
                canvas.DrawLine(x1, yLine, x2, yLine);
            }
        }

        if (CurrentFrame >= 0 && CurrentFrame < _frameCount)
        {
            float x = CurrentFrame * cellWidth + cellWidth / 2f;
            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 2;
            canvas.DrawLine(x, 0, x, _bones.Count * (rowHeight + rowSpacing));
        }
    }

}

internal class TimelineHeaderDrawable : IDrawable
{
    private int _frameCount = 20;

    public float ScrollX { get; set; }
    public int CurrentFrame { get; set; }

    public void SetFrameCount(int count) => _frameCount = Math.Max(1, count);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float cellWidth = 20f;
        const float headerHeight = 20f;

        canvas.Translate(-ScrollX, 0);

        int startCol = Math.Max(0, (int)(ScrollX / cellWidth));
        int endCol = Math.Min(_frameCount - 1,
            (int)((ScrollX + dirtyRect.Width) / cellWidth) + 1);

        for (int c = startCol; c <= endCol; c++)
        {
            float x = c * cellWidth;
            canvas.FillColor = (c % 2 == 0) ? Color.FromArgb("#303030") : Color.FromArgb("#202020");
            canvas.FillRectangle(x, 0, cellWidth, headerHeight);

            canvas.FontColor = Colors.White;
            canvas.FontSize = 10;
            canvas.DrawString(c.ToString(), x, 0, cellWidth, headerHeight,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        if (CurrentFrame >= 0 && CurrentFrame < _frameCount)
        {
            float x = CurrentFrame * cellWidth + cellWidth / 2f;
            canvas.FillColor = Colors.Black;
            var path = new PathF();
            path.MoveTo(x - 4, headerHeight);
            path.LineTo(x + 4, headerHeight);
            path.LineTo(x, headerHeight - 6);
            path.Close();
            canvas.FillPath(path);

            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 2;
            canvas.DrawLine(x, headerHeight, x, headerHeight + 6);
        }
    }
}
