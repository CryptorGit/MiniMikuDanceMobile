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
        UpdateConfirmEnabled();
    }

    public void SetBones(IEnumerable<string> bones)
    {
        var boneList = bones.ToList();
        BonePicker.ItemsSource = boneList;
        if (boneList.Any())
            BonePicker.SelectedIndex = 0;
        UpdateConfirmEnabled();
    }

    public void SetFrame(int frame, bool isEditMode = false, Func<string, int, Vector3>? getTranslation = null, Func<string, int, Vector3>? getRotation = null)
    {
        FrameEntry.Text = frame.ToString();

        if (isEditMode && getTranslation != null && getRotation != null)
        {
            var boneName = SelectedBone;
            if (!string.IsNullOrEmpty(boneName))
            {
                SetTranslation(getTranslation(boneName, frame));
                SetRotation(getRotation(boneName, frame));
            }
        }
    }

    public int FrameNumber => int.TryParse(FrameEntry.Text, out var f) ? f : 0;
    public int SelectedBoneIndex => BonePicker.SelectedIndex;
    public string SelectedBone => BonePicker.SelectedItem as string ?? string.Empty;

    public Vector3 Translation => new((float)PosXSlider.Value, (float)PosYSlider.Value, (float)PosZSlider.Value);
    public Vector3 EulerRotation => new((float)RotXSlider.Value, (float)RotYSlider.Value, (float)RotZSlider.Value);

    public void SetTranslation(Vector3 t)
    {
        PosXSlider.Value = t.X;
        PosYSlider.Value = t.Y;
        PosZSlider.Value = t.Z;
        PosXLabel.Text = $"{t.X:F2}";
        PosYLabel.Text = $"{t.Y:F2}";
        PosZLabel.Text = $"{t.Z:F2}";
    }

    public void SetRotation(Vector3 r)
    {
        RotXSlider.Value = r.X;
        RotYSlider.Value = r.Y;
        RotZSlider.Value = r.Z;
        RotXLabel.Text = $"{r.X:F0}";
        RotYLabel.Text = $"{r.Y:F0}";
        RotZLabel.Text = $"{r.Z:F0}";
    }

    private void OnConfirmClicked(object? sender, EventArgs e)
        => Confirmed?.Invoke(SelectedBone, FrameNumber, Translation, EulerRotation);

    private void OnCancelClicked(object? sender, EventArgs e)
        => Canceled?.Invoke();

    private void UpdateConfirmEnabled()
    {
        // Assuming the Apply button is named ApplyButton in XAML
        var applyButton = this.FindByName<Button>("ApplyButton");
        if (applyButton != null)
        {
            applyButton.IsEnabled = BonePicker.SelectedIndex >= 0 && int.TryParse(FrameEntry.Text, out _);
        }
    }

    private void OnFrameTextChanged(object? sender, TextChangedEventArgs e)
        => UpdateConfirmEnabled();

    private void OnBoneChanged(object? sender, EventArgs e)
    {
        BoneChanged?.Invoke(BonePicker.SelectedIndex);
        UpdateConfirmEnabled();
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

    // Slider ValueChanged handlers
    private void OnPosXChanged(object? sender, ValueChangedEventArgs e) => PosXLabel.Text = $"{e.NewValue:F2}";
    private void OnPosYChanged(object? sender, ValueChangedEventArgs e) => PosYLabel.Text = $"{e.NewValue:F2}";
    private void OnPosZChanged(object? sender, ValueChangedEventArgs e) => PosZLabel.Text = $"{e.NewValue:F2}";
    private void OnRotXChanged(object? sender, ValueChangedEventArgs e) => RotXLabel.Text = $"{e.NewValue:F0}";
    private void OnRotYChanged(object? sender, ValueChangedEventArgs e) => RotYLabel.Text = $"{e.NewValue:F0}";
    private void OnRotZChanged(object? sender, ValueChangedEventArgs e) => RotZLabel.Text = $"{e.NewValue:F0}";

    // Reset Button Clicked handlers
    private void OnResetRotXClicked(object? sender, EventArgs e) => RotXSlider.Value = 0;
    private void OnResetRotYClicked(object? sender, EventArgs e) => RotYSlider.Value = 0;
    private void OnResetRotZClicked(object? sender, EventArgs e) => RotZSlider.Value = 0;
    private void OnResetPosXClicked(object? sender, EventArgs e) => PosXSlider.Value = 0;
    private void OnResetPosYClicked(object? sender, EventArgs e) => PosYSlider.Value = 0;
    private void OnResetPosZClicked(object? sender, EventArgs e) => PosZSlider.Value = 0;
}
