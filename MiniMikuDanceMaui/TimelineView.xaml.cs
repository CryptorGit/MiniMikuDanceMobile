using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;
using MauiIcons.Core;
using MauiIcons.Material.Outlined;

namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
    public event Action? PlayRequested;
    public event Action<int>? FrameChanged;
    public event Action? AddKeyRequested;

    private bool _isPlaying;
    private const int DefaultFrameColumns = 20;
    private int _frameCount = DefaultFrameColumns;
    private const int RowHeight = 24;
    public const int RowSpacing = 4;
    private int _currentFrame;
    private bool _suppressScroll;
    private readonly Label _cursorArrow;
    private bool _initialized;
    private readonly TimelineDrawable _drawable = new();

    public TimelineView()
    {
        InitializeComponent();
        // URL スタイルの名前空間利用時に必要な一時的ワークアラウンド
        _ = new MauiIcon();
        BoneList.Spacing = RowSpacing;
        TimelineCanvas.Drawable = _drawable;
        _cursorArrow = new Label
        {
            Text = "▲",
            FontSize = 10,
            TextColor = Colors.Black,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            InputTransparent = true,
            Margin = new Thickness(0),
            Padding = new Thickness(0)
        };
    }

    public void SetFrameCount(int count)
    {
        _frameCount = Math.Max(1, count);
        TimelineSlider.Maximum = _frameCount > 0 ? _frameCount - 1 : 0;

        _drawable.SetFrameCount(_frameCount);
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

    private void BuildTimeline()
    {
        FrameHeader.ColumnDefinitions.Clear();
        for (int c = 0; c < _frameCount; c++)
        {
            FrameHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = 20 });
        }

        FrameHeader.Children.Clear();
        for (int c = 0; c < _frameCount; c++)
        {
            var headerLabel = new Label
            {
                Text = c.ToString(),
                FontSize = 10,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                BackgroundColor = (c % 2 == 0) ? Color.FromArgb("#303030") : Color.FromArgb("#202020"),
                Margin = new Thickness(0),
                Padding = new Thickness(0)
            };
            var headerTap = new TapGestureRecognizer();
            int captured = c;
            headerTap.Tapped += (_, _) => SetFrameIndex(captured);
            headerLabel.GestureRecognizers.Add(headerTap);
            FrameHeader.Add(headerLabel, c, 0);
        }

        if (!_initialized)
        {
            FrameHeader.Add(_cursorArrow, _currentFrame, 0);
            _initialized = true;
        }

        TimelineCanvas.WidthRequest = 20 * _frameCount;
        TimelineCanvas.HeightRequest = _drawable.BoneCount * RowHeight + (_drawable.BoneCount - 1) * RowSpacing;
        UpdateCurrentIndicator();
    }

    private void UpdateCurrentIndicator()
    {
        if (!_initialized)
            return;
        Grid.SetColumn(_cursorArrow, _currentFrame);
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

    private void OnTimelineStartInteraction(object? sender, TouchEventArgs e)
    {
        var p = e.Touches[0];
        int frame = (int)((p.X + TimelineScrollView.ScrollX) / 20);
        SetFrameIndex(frame);
    }

    private void OnTimelineEndInteraction(object? sender, TouchEventArgs e)
    {
    }


    private async void OnBoneListScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_suppressScroll)
            return;

        _suppressScroll = true;
        await TimelineScrollView.ScrollToAsync(TimelineScrollView.ScrollX, e.ScrollY, false);
        _suppressScroll = false;
        _drawable.ScrollY = (float)e.ScrollY;
        TimelineCanvas.Invalidate();
    }

    private async void OnTimelineScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_suppressScroll)
            return;

        _suppressScroll = true;
        await BoneListScrollView.ScrollToAsync(0, e.ScrollY, false);
        _suppressScroll = false;
        _drawable.ScrollX = (float)e.ScrollX;
        _drawable.ScrollY = (float)e.ScrollY;
        TimelineCanvas.Invalidate();
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

    public void AddKeyFrame(string bone, int frame)
    {
        int index = _bones.IndexOf(bone);
        if (index >= 0)
            _keyFrames[index].Add(frame);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float cellWidth = 20f;
        const float rowHeight = 24f;
        const float rowSpacing = TimelineView.RowSpacing;

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

                if (_keyFrames[r].Contains(c))
                {
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
                else
                {
                    var keys = _keyFrames[r].OrderBy(i => i).ToList();
                    for (int i = 0; i < keys.Count - 1; i++)
                    {
                        if (c > keys[i] && c < keys[i + 1])
                        {
                            canvas.FillColor = Colors.Orange;
                            canvas.FillRectangle(x, y + rowHeight / 2f - 1.5f, cellWidth, 3);
                            break;
                        }
                    }
                }
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
