using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;

namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
    public event Action? PlayRequested;
    public event Action<int>? FrameChanged;
    public event Action? AddKeyRequested;

    private const int DefaultFrameCount = 20;
    private int _frameCount = DefaultFrameCount;
    private int _currentFrame;
    private readonly Dictionary<int, HashSet<string>> _keyFrames = new();
    private bool _isPlaying;

    public TimelineView()
    {
        InitializeComponent();
        BuildTimeline();
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
        UpdateHighlight();
    }

    public void UpdatePlayState(bool playing)
    {
        _isPlaying = playing;
        PlayButton.Text = playing ? "⏸" : "▶";
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
                LineBreakMode = LineBreakMode.NoWrap
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
        if (!_keyFrames.TryGetValue(frame, out var set))
        {
            set = new HashSet<string>();
            _keyFrames[frame] = set;
        }
        set.Add(bone);
        BuildTimeline();
        SetFrameIndex(frame);
    }

    private void BuildTimeline()
    {
        FrameContainer.Children.Clear();
        for (int i = 0; i < _frameCount; i++)
        {
            FrameHeader.Add(new Label
            {
                Text = i.ToString(),
                FontSize = 10,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true,
                BackgroundColor = (c % 2 == 0) ? Color.FromArgb("#303030") : Color.FromArgb("#202020")
            }, c, 0);
        }

        TimelineGrid.Children.Clear();
        for (int r = 0; r < _keyFrames.Count; r++)
        {
            var keys = _keyFrames[r].OrderBy(i => i).ToList();
            for (int c = 0; c < _frameCount; c++)
            {
                cell.Children.Add(new Label
                {
                    Text = "◆",
                    FontSize = 10,
                    TextColor = Colors.Orange,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.End
                });
            }
            var tap = new TapGestureRecognizer();
            int captured = i;
            tap.Tapped += (_, _) => OnFrameTapped(captured);
            cell.GestureRecognizers.Add(tap);
            FrameContainer.Add(cell);
        }
        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        foreach (var child in FrameContainer.Children)
        {
            if (child is VisualElement ve)
            {
                ve.BackgroundColor = ve.StyleId == _currentFrame.ToString()
                    ? Color.FromArgb("#555555")
                    : Color.FromArgb("#303030");
            }
        }
    }

    private void OnFrameTapped(int frame)
    {
        SetFrameIndex(frame);
        FrameChanged?.Invoke(frame);
    }

    private void OnPlayClicked(object? sender, EventArgs e) => PlayRequested?.Invoke();

    private void OnAddKeyClicked(object? sender, EventArgs e) => AddKeyRequested?.Invoke();

    private void OnSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        _currentFrame = (int)e.NewValue;
        FrameChanged?.Invoke(_currentFrame);
        UpdateHighlight();
    }
}
