using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace MiniMikuDanceMaui;

public partial class KeyInputPanel : ContentView
{
    public event Action<string, int, Vector3, Vector3>? Confirmed;
    public event Action? Canceled;
    public event Action<int>? BoneChanged;

    public KeyInputPanel()
    {
        InitializeComponent();
        PosRangePicker.ItemsSource = new List<int> { 1, 2, 5, 10 };
        PosRangePicker.SelectedItem = 1;
        RotRangePicker.ItemsSource = new List<int> { 30, 45, 90, 180, 360 };
        RotRangePicker.SelectedItem = 180;
        OnPosRangeChanged(null, EventArgs.Empty);
        OnRotRangeChanged(null, EventArgs.Empty);
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BonePicker.ItemsSource = bones.ToList();
        if (BonePicker.ItemsSource.Cast<object>().Any())
            BonePicker.SelectedIndex = 0;
    }

    public void SetFrame(int frame)
        => FrameEntry.Text = frame.ToString();

    public int Frame => int.TryParse(FrameEntry.Text, out var f) ? f : 0;
    public string SelectedBone => BonePicker.SelectedItem as string ?? string.Empty;
    public int SelectedBoneIndex => BonePicker.SelectedIndex;

    public Vector3 Translation => new((float)PosXSlider.Value, (float)PosYSlider.Value, (float)PosZSlider.Value);
    public Vector3 Rotation => new((float)RotXSlider.Value, (float)RotYSlider.Value, (float)RotZSlider.Value);

    public void SetTranslation(Vector3 t)
    {
        PosXSlider.Value = t.X; PosYSlider.Value = t.Y; PosZSlider.Value = t.Z;
        PosXLabel.Text = $"{t.X:F2}"; PosYLabel.Text = $"{t.Y:F2}"; PosZLabel.Text = $"{t.Z:F2}";
    }

    public void SetRotation(Vector3 r)
    {
        RotXSlider.Value = r.X; RotYSlider.Value = r.Y; RotZSlider.Value = r.Z;
        RotXLabel.Text = $"{r.X:F0}"; RotYLabel.Text = $"{r.Y:F0}"; RotZLabel.Text = $"{r.Z:F0}";
    }

    private void OnConfirmClicked(object? sender, EventArgs e)
        => Confirmed?.Invoke(SelectedBone, Frame, Translation, Rotation);

    private void OnCancelClicked(object? sender, EventArgs e)
        => Canceled?.Invoke();

    private void OnFrameMinusClicked(object? sender, EventArgs e)
    {
        if (int.TryParse(FrameEntry.Text, out var value))
            FrameEntry.Text = (value - 1).ToString();
        else
            FrameEntry.Text = "0";
    }

    private void OnFramePlusClicked(object? sender, EventArgs e)
    {
        if (int.TryParse(FrameEntry.Text, out var value))
            FrameEntry.Text = (value + 1).ToString();
        else
            FrameEntry.Text = "0";
    }

    private void OnRotRangeChanged(object? sender, EventArgs e)
    {
        if (RotRangePicker.SelectedItem is int range)
        {
            RotXSlider.Minimum = -range; RotXSlider.Maximum = range;
            RotYSlider.Minimum = -range; RotYSlider.Maximum = range;
            RotZSlider.Minimum = -range; RotZSlider.Maximum = range;
        }
    }

    private void OnPosRangeChanged(object? sender, EventArgs e)
    {
        if (PosRangePicker.SelectedItem is int range)
        {
            PosXSlider.Minimum = -range; PosXSlider.Maximum = range;
            PosYSlider.Minimum = -range; PosYSlider.Maximum = range;
            PosZSlider.Minimum = -range; PosZSlider.Maximum = range;
        }
    }

    private void OnPosXChanged(object? sender, ValueChangedEventArgs e)
        => PosXLabel.Text = $"{e.NewValue:F2}";
    private void OnPosYChanged(object? sender, ValueChangedEventArgs e)
        => PosYLabel.Text = $"{e.NewValue:F2}";
    private void OnPosZChanged(object? sender, ValueChangedEventArgs e)
        => PosZLabel.Text = $"{e.NewValue:F2}";
    private void OnRotXChanged(object? sender, ValueChangedEventArgs e)
        => RotXLabel.Text = $"{e.NewValue:F0}";
    private void OnRotYChanged(object? sender, ValueChangedEventArgs e)
        => RotYLabel.Text = $"{e.NewValue:F0}";
    private void OnRotZChanged(object? sender, ValueChangedEventArgs e)
        => RotZLabel.Text = $"{e.NewValue:F0}";

    private void OnBoneChanged(object? sender, EventArgs e)
        => BoneChanged?.Invoke(BonePicker.SelectedIndex);
}
