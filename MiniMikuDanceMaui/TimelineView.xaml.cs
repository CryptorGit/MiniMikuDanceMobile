using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;
using MauiIcons.Core;
using MauiIcons.Material.Outlined;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
public event Action? PlayRequested;
public event Action<int>? FrameChanged;
public event Action? AddKeyRequested;
    private bool _isPlaying;
    private const int DefaultFrameColumns = 20;
    private int _frameCount = DefaultFrameColumns;
    private readonly List<HashSet<int>> _keyFrames = new();
    private readonly List<string> _bones = new();
    private const int RowHeight = 24;
    public const int RowSpacing = 4;
    private int _currentFrame;
    private bool _suppressScroll;
    private readonly BoxView _cursorLine;
    private readonly Label _cursorArrow;
    private bool _initialized;
    private readonly SKPaint _gridPaint = new() { Color = SKColors.Gray, StrokeWidth = 1 };
    private readonly SKPaint _keyPaint = new() { Color = SKColors.Orange };

    public TimelineView()
    {
        InitializeComponent();
        // URL スタイルの名前空間利用時に必要な一時的ワークアラウンド
        _ = new MauiIcon();
        BoneList.Spacing = RowSpacing;
        TimelineGrid.RowSpacing = RowSpacing;
        _cursorLine = new BoxView
        {
            Color = Colors.Red,
            WidthRequest = 2,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = true
        };
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

        BuildTimeline();
    }

    public void SetFrameIndex(int index)
    {
        _currentFrame = index;
        if ((int)TimelineSlider.Value != index)
            TimelineSlider.Value = index;
        UpdateCurrentIndicator();
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
        TimelineGrid.RowDefinitions.Clear();
        _keyFrames.Clear();
        _bones.Clear();

        int row = 0;
        foreach (var name in bones)
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
            TimelineGrid.RowDefinitions.Add(new RowDefinition { Height = RowHeight });
            _keyFrames.Add(new HashSet<int>());
            _bones.Add(name);
            row++;
        }

        SetFrameCount(_frameCount);
    }

    public void AddKeyFrame(string bone, int frame)
    {
        int index = _bones.IndexOf(bone);
        if (index >= 0 && index < _keyFrames.Count)
        {
            _keyFrames[index].Add(frame);
            BuildTimeline();
            SetFrameIndex(frame);
        }
    }

    private void BuildTimeline()
    {
        TimelineGrid.ColumnDefinitions.Clear();
        FrameHeader.ColumnDefinitions.Clear();
        for (int c = 0; c < _frameCount; c++)
        {
            TimelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 20 });
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

        TimelineGrid.Children.Clear();
        TimelineCanvas.InvalidateSurface();

        if (!_initialized)
        {
            TimelineGrid.Add(_cursorLine, _currentFrame, 0);
            Grid.SetRowSpan(_cursorLine, _keyFrames.Count);
            FrameHeader.Add(_cursorArrow, _currentFrame, 0);
            _initialized = true;
        }
        UpdateCurrentIndicator();
    }

    private void UpdateCurrentIndicator()
    {
        if (!_initialized)
            return;
        Grid.SetColumn(_cursorLine, _currentFrame);
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
    }


    private async void OnBoneListScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_suppressScroll)
            return;

        _suppressScroll = true;
        await TimelineScrollView.ScrollToAsync(TimelineScrollView.ScrollX, e.ScrollY, false);
        _suppressScroll = false;
    }

    private async void OnTimelineScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_suppressScroll)
            return;

        _suppressScroll = true;
        await BoneListScrollView.ScrollToAsync(0, e.ScrollY, false);
        _suppressScroll = false;
    }

    private void OnTimelineCanvasPaint(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        float cellWidth = 20;
        float cellHeight = RowHeight + RowSpacing;
        for (int r = 0; r < _keyFrames.Count; r++)
        {
            for (int c = 0; c < _frameCount; c++)
            {
                float x = c * cellWidth;
                float y = r * cellHeight;
                canvas.DrawRect(x, y, cellWidth, RowHeight, _gridPaint);
                if (_keyFrames[r].Contains(c))
                {
                    canvas.DrawText("◆", x + cellWidth / 2, y + RowHeight / 2, _keyPaint);
                }
            }
        }
    }

    private void OnCanvasTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType != SKTouchAction.Released)
            return;

        float cellWidth = 20;
        float cellHeight = RowHeight + RowSpacing;
        int column = (int)(e.Location.X / cellWidth);
        int row = (int)(e.Location.Y / cellHeight);
        if (row >= 0 && row < _keyFrames.Count && column >= 0 && column < _frameCount)
        {
            if (_keyFrames[row].Contains(column))
                _keyFrames[row].Remove(column);
            else
                _keyFrames[row].Add(column);

            SetFrameIndex(column);
            TimelineCanvas.InvalidateSurface();
        }
        e.Handled = true;
    }
}
