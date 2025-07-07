using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace MiniMikuDanceMaui;

public partial class BoneView : ContentView
{
    public event Action<int>? BoneSelected;
    public event Action<float>? RotationXChanged;
    public event Action<float>? RotationYChanged;
    public event Action<float>? RotationZChanged;
    public event Action<float>? TranslationXChanged;
    public event Action<float>? TranslationYChanged;
    public event Action<float>? TranslationZChanged;
    public event Action? ResetRequested;
    public event Action<int>? RotateRangeChanged;
    public event Action<int>? PositionRangeChanged;

    private int _range = 180;

    public BoneView()
    {
        InitializeComponent();
        RotateRangePicker.ItemsSource = new List<int> { 30, 45, 90, 180, 360 };
        RotateRangePicker.SelectedItem = _range;
        PositionRangePicker.ItemsSource = new List<int> { 1, 2, 5, 10 };
        PositionRangePicker.SelectedItem = 1;
        LabelX.Text = "0";
        LabelY.Text = "0";
        LabelZ.Text = "0";
        LabelTX.Text = "0";
        LabelTY.Text = "0";
        LabelTZ.Text = "0";
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BonePicker.ItemsSource = bones.ToList();
        if (BonePicker.ItemsSource.Cast<object>().Any())
            BonePicker.SelectedIndex = 0;
    }

    public void SetRotation(OpenTK.Mathematics.Vector3 degrees)
    {
        RotationX = degrees.X;
        RotationY = degrees.Y;
        RotationZ = degrees.Z;
        LabelX.Text = $"{degrees.X:F0}";
        LabelY.Text = $"{degrees.Y:F0}";
        LabelZ.Text = $"{degrees.Z:F0}";
    }

    public void SetTranslation(OpenTK.Mathematics.Vector3 t)
    {
        TranslationX = t.X;
        TranslationY = t.Y;
        TranslationZ = t.Z;
        LabelTX.Text = $"{t.X:F2}";
        LabelTY.Text = $"{t.Y:F2}";
        LabelTZ.Text = $"{t.Z:F2}";
    }

    private void OnBoneSelected(object? sender, EventArgs e)
    {
        if (BonePicker.SelectedIndex >= 0)
            BoneSelected?.Invoke(BonePicker.SelectedIndex);
    }

    private void OnXChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelX.Text = $"{e.NewValue:F0}";
        RotationXChanged?.Invoke((float)e.NewValue);
    }

    private void OnYChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelY.Text = $"{e.NewValue:F0}";
        RotationYChanged?.Invoke((float)e.NewValue);
    }

    private void OnZChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelZ.Text = $"{e.NewValue:F0}";
        RotationZChanged?.Invoke((float)e.NewValue);
    }

    private void OnTXChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelTX.Text = $"{e.NewValue:F2}";
        TranslationXChanged?.Invoke((float)e.NewValue);
    }

    private void OnTYChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelTY.Text = $"{e.NewValue:F2}";
        TranslationYChanged?.Invoke((float)e.NewValue);
    }

    private void OnTZChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelTZ.Text = $"{e.NewValue:F2}";
        TranslationZChanged?.Invoke((float)e.NewValue);
    }

    private void OnResetClicked(object? sender, EventArgs e) => ResetRequested?.Invoke();

    private void OnRotateRangeChanged(object? sender, EventArgs e)
    {
        if (RotateRangePicker.SelectedItem is int value)
        {
            _range = value;
            RotateRangeChanged?.Invoke(value);
        }
    }

    private void OnPositionRangeChanged(object? sender, EventArgs e)
    {
        if (PositionRangePicker.SelectedItem is int value)
        {
            SliderTX.Minimum = -value;
            SliderTX.Maximum = value;
            SliderTY.Minimum = -value;
            SliderTY.Maximum = value;
            SliderTZ.Minimum = -value;
            SliderTZ.Maximum = value;
            PositionRangeChanged?.Invoke(value);
        }
    }

    public new float RotationX
    {
        get => (float)SliderX.Value;
        set => SliderX.Value = value;
    }

    public new float RotationY
    {
        get => (float)SliderY.Value;
        set => SliderY.Value = value;
    }

    public float RotationZ
    {
        get => (float)SliderZ.Value;
        set => SliderZ.Value = value;
    }

    public new float TranslationX
    {
        get => (float)SliderTX.Value;
        set => SliderTX.Value = value;
    }

    public new float TranslationY
    {
        get => (float)SliderTY.Value;
        set => SliderTY.Value = value;
    }

    public new float TranslationZ
    {
        get => (float)SliderTZ.Value;
        set => SliderTZ.Value = value;
    }

    public void SetRotationRange(int min, int max)
    {
        SliderX.Minimum = min;
        SliderX.Maximum = max;
        SliderY.Minimum = min;
        SliderY.Maximum = max;
        SliderZ.Minimum = min;
        SliderZ.Maximum = max;
    }

    public void SetTranslationRange(int min, int max)
    {
        SliderTX.Minimum = min;
        SliderTX.Maximum = max;
        SliderTY.Minimum = min;
        SliderTY.Maximum = max;
        SliderTZ.Minimum = min;
        SliderTZ.Maximum = max;
    }
}
