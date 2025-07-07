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
        // Simplified view does not show bones, method retained for compatibility
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
            var cell = new Grid { StyleId = i.ToString(), WidthRequest = 24, HeightRequest = 40, BackgroundColor = Color.FromArgb("#303030") };
            var label = new Label
            {
                Text = i.ToString(),
                FontSize = 10,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start
            };
            cell.Children.Add(label);
            if (_keyFrames.ContainsKey(i))
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
