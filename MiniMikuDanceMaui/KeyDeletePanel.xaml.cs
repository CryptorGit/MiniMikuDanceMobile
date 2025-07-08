using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMikuDanceMaui;

public partial class KeyDeletePanel : ContentView
{
    public event Action<string, int>? Confirmed;
    public event Action? Canceled;
    public event Action<int>? BoneChanged;

    public KeyDeletePanel()
    {
        InitializeComponent();
        UpdateDeleteEnabled();
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BonePicker.ItemsSource = bones.ToList();
        if (BonePicker.ItemsSource.Cast<object>().Any())
            BonePicker.SelectedIndex = 0;
        UpdateDeleteEnabled();
    }

    public void SetFrames(IEnumerable<int> frames)
    {
        FramePicker.ItemsSource = frames.ToList();
        if (FramePicker.ItemsSource.Cast<object>().Any())
            FramePicker.SelectedIndex = 0;
        UpdateDeleteEnabled();
    }

    public string SelectedBone => BonePicker.SelectedItem as string ?? string.Empty;
    public int SelectedBoneIndex => BonePicker.SelectedIndex;
    public int SelectedFrame => FramePicker.SelectedItem is int f ? f : 0;

    private void UpdateDeleteEnabled()
        => DeleteButton.IsEnabled = BonePicker.SelectedIndex >= 0 && FramePicker.SelectedIndex >= 0;

    private void OnFrameChanged(object? sender, EventArgs e)
        => UpdateDeleteEnabled();

    private void OnDeleteClicked(object? sender, EventArgs e)
        => Confirmed?.Invoke(SelectedBone, SelectedFrame);

    private void OnCancelClicked(object? sender, EventArgs e)
        => Canceled?.Invoke();

    private void OnBoneChanged(object? sender, EventArgs e)
    {
        BoneChanged?.Invoke(BonePicker.SelectedIndex);
        UpdateDeleteEnabled();
    }
}
