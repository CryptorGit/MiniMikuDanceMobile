using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace MiniMikuDanceMaui;

public partial class AnimationView : ContentView
{
    public event Action? PlayRequested;
    public event Action<int>? FrameChanged;
    private bool _isPlaying;

    private const int DefaultFrameColumns = 20;

    public AnimationView()
    {
        InitializeComponent();
    }

    public void SetFrameCount(int count)
    {
        TimelineSlider.Maximum = count > 0 ? count - 1 : 0;
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
        TimelineGrid.ColumnDefinitions.Clear();
        for (int c = 0; c < DefaultFrameColumns; c++)
        {
            TimelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 20 });
        }
        int row = 0;
        foreach (var name in bones)
        {
            BoneList.Children.Add(new Label { Text = name, TextColor = Colors.White, FontSize = 12 });
            TimelineGrid.RowDefinitions.Add(new RowDefinition { Height = 24 });
            var box = new BoxView { Color = Colors.Orange, WidthRequest = 10, HeightRequest = 10, HorizontalOptions = LayoutOptions.Start };
            TimelineGrid.Add(box, 0, row);
            row++;
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
}
