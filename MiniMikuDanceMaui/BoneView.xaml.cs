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
    public event Action<int>? RangeChanged;

    private int _range = 180;

    public BoneView()
    {
        InitializeComponent();
        RangePicker.ItemsSource = new List<int> { 30, 45, 90, 180, 360 };
        RangePicker.SelectedItem = _range;
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
    }

    public void SetTranslation(OpenTK.Mathematics.Vector3 t)
    {
        TranslationX = t.X;
        TranslationY = t.Y;
        TranslationZ = t.Z;
    }

    private void OnBoneSelected(object? sender, EventArgs e)
    {
        if (BonePicker.SelectedIndex >= 0)
            BoneSelected?.Invoke(BonePicker.SelectedIndex);
    }

    private void OnXChanged(object? sender, ValueChangedEventArgs e)
        => RotationXChanged?.Invoke((float)e.NewValue);

    private void OnYChanged(object? sender, ValueChangedEventArgs e)
        => RotationYChanged?.Invoke((float)e.NewValue);

    private void OnZChanged(object? sender, ValueChangedEventArgs e)
        => RotationZChanged?.Invoke((float)e.NewValue);

    private void OnTXChanged(object? sender, ValueChangedEventArgs e)
        => TranslationXChanged?.Invoke((float)e.NewValue);

    private void OnTYChanged(object? sender, ValueChangedEventArgs e)
        => TranslationYChanged?.Invoke((float)e.NewValue);

    private void OnTZChanged(object? sender, ValueChangedEventArgs e)
        => TranslationZChanged?.Invoke((float)e.NewValue);

    private void OnResetClicked(object? sender, EventArgs e) => ResetRequested?.Invoke();

    private void OnRangeChanged(object? sender, EventArgs e)
    {
        if (RangePicker.SelectedItem is int value)
        {
            _range = value;
            RangeChanged?.Invoke(value);
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

    public float TranslationX
    {
        get => (float)SliderTX.Value;
        set => SliderTX.Value = value;
    }

    public float TranslationY
    {
        get => (float)SliderTY.Value;
        set => SliderTY.Value = value;
    }

    public float TranslationZ
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
}
