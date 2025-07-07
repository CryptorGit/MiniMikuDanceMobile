using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class AnimationView : ContentView
{
    public event Action? PlayRequested;
    public event Action<int>? FrameChanged;
    private bool _isPlaying;

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

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        PlayRequested?.Invoke();
    }

    private void OnSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        FrameChanged?.Invoke((int)e.NewValue);
    }
}
