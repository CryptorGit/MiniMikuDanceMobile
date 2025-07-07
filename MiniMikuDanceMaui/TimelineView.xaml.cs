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

    public TimelineView()
    {
        InitializeComponent();
    }

    public void SetFrameCount(int count)
    {
        _frameCount = Math.Max(1, count);
        TimelineSlider.Maximum = _frameCount > 0 ? _frameCount - 1 : 0;

        TimelineGrid.ColumnDefinitions.Clear();
        for (int c = 0; c < _frameCount; c++)
        {
            TimelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 20 });
        }
        UpdateTimeline();
    }

    public void SetFrameIndex(int index)
    {
        if ((int)TimelineSlider.Value != index)
            TimelineSlider.Value = index;
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

        int row = 0;
        foreach (var name in bones)
        {
            BoneList.Children.Add(new Label { Text = name, TextColor = Colors.White, FontSize = 12 });
            TimelineGrid.RowDefinitions.Add(new RowDefinition { Height = 24 });
            _keyFrames.Add(new HashSet<int>());
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
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        PlayRequested?.Invoke();
    }

    private void OnSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        FrameChanged?.Invoke((int)e.NewValue);
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

                    UpdateTimeline();
                }
            }
        }
    }
}
