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
        var list = bones.ToList();
        BonePicker.ItemsSource = list;
        BonePicker.SelectedIndex = list.Any() ? 0 : -1;
        UpdateDeleteEnabled();
    }

    public void SetFrames(IEnumerable<int> frames)
    {
        var list = frames.ToList();
        FramePicker.ItemsSource = list;
        FramePicker.SelectedIndex = list.Any() ? 0 : -1;
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
