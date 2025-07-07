using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;

namespace MiniMikuDanceMaui;

public partial class TimelineView : ContentView
{
    public event Action? PlayRequested;
    public event Action<int>? FrameChanged;
    private bool _isPlaying;
    private const int DefaultFrameColumns = 20;
    private int _frameCount = DefaultFrameColumns;
    private readonly List<HashSet<int>> _keyFrames = new();
    private readonly List<string> _bones = new();
    private int _currentFrame;

    public TimelineView()
    {
        InitializeComponent();
    }

    public void SetFrameCount(int count)
    {
        _frameCount = Math.Max(1, count);
        TimelineSlider.Maximum = _frameCount > 0 ? _frameCount - 1 : 0;

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
            FrameHeader.Add(new Label
            {
                Text = c.ToString(),
                FontSize = 10,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true
            }, c, 0);
        }
        UpdateTimeline();
    }

    public void SetFrameIndex(int index)
    {
        _currentFrame = index;
        if ((int)TimelineSlider.Value != index)
            TimelineSlider.Value = index;
        UpdateTimeline();
    }

    public void UpdatePlayState(bool playing)
    {
        _isPlaying = playing;
        PlayButton.Text = playing ? "Pause" : "Play";
    }

    public void SetKeyInputPanelVisible(bool visible)
    {
        KeyInputPanel.IsVisible = visible;
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
            BoneList.Children.Add(new Label { Text = name, TextColor = Colors.White, FontSize = 12 });
            TimelineGrid.RowDefinitions.Add(new RowDefinition { Height = 24 });
            _keyFrames.Add(new HashSet<int>());
            _bones.Add(name);
            row++;
        }

        SetFrameCount(_frameCount);
    }

    private void UpdateTimeline()
    {
        TimelineGrid.Children.Clear();

        for (int r = 0; r < _keyFrames.Count; r++)
        {
            var keys = _keyFrames[r].OrderBy(i => i).ToList();
            for (int c = 0; c < _frameCount; c++)
            {
                var cell = new Grid { StyleId = $"{r}_{c}" };
                var tap = new TapGestureRecognizer();
                tap.Tapped += OnCellTapped;
                cell.GestureRecognizers.Add(tap);

                if (_keyFrames[r].Contains(c))
                {
                    cell.Children.Add(new Label
                    {
                        Text = "◆",
                        FontSize = 10,
                        TextColor = Colors.Orange,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        InputTransparent = true
                    });
                }
                else
                {
                    for (int i = 0; i < keys.Count - 1; i++)
                    {
                        if (c > keys[i] && c < keys[i + 1])
                        {
                            cell.Children.Add(new BoxView
                            {
                                Color = Colors.Orange,
                                HeightRequest = 3,
                                HorizontalOptions = LayoutOptions.Fill,
                                VerticalOptions = LayoutOptions.Center,
                                InputTransparent = true
                            });
                            break;
                        }
                    }
                }

                TimelineGrid.Add(cell, c, r);
            }
        }

        if (_currentFrame >= 0 && _currentFrame < _frameCount)
        {
            FrameHeader.Children.Clear();
            for (int c = 0; c < _frameCount; c++)
            {
                FrameHeader.Add(new Label
                {
                    Text = c.ToString(),
                    FontSize = 10,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    InputTransparent = true
                }, c, 0);
            }

            var line = new BoxView
            {
                Color = Colors.Red,
                WidthRequest = 2,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Fill,
                InputTransparent = true
            };
            TimelineGrid.Add(line, _currentFrame, 0);
            Grid.SetRowSpan(line, _keyFrames.Count);

            FrameHeader.Add(new Label
            {
                Text = "▲",
                FontSize = 10,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                InputTransparent = true
            }, _currentFrame, 0);
        }
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        PlayRequested?.Invoke();
    }

    private void OnSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        _currentFrame = (int)e.NewValue;
        FrameChanged?.Invoke(_currentFrame);
        UpdateTimeline();
    }

    private void OnCellTapped(object? sender, TappedEventArgs e)
    {
        if (sender is VisualElement ve && ve.StyleId != null)
        {
            var parts = ve.StyleId.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[0], out var r) && int.TryParse(parts[1], out var c))
            {
                if (_keyFrames.Count > r)
                {
                    if (_keyFrames[r].Contains(c))
                        _keyFrames[r].Remove(c);
                    else
                        _keyFrames[r].Add(c);

                    SetFrameIndex(c);
                    if (_bones.Count > r)
                        BoneEntry.Text = _bones[r];
                    FrameEntry.Text = c.ToString();
                    SetKeyInputPanelVisible(true);
                }
            }
        }
    }
}
