using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMikuDanceMaui;

public partial class BoneView : ContentView
{
    public event Action<string>? BoneSelected;
    public event Action<float>? RotationXChanged;
    public event Action<float>? RotationYChanged;
    public event Action<float>? RotationZChanged;

    public BoneView()
    {
        InitializeComponent();
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BonePicker.ItemsSource = bones.ToList();
        if (BonePicker.ItemsSource.Cast<object>().Any())
            BonePicker.SelectedIndex = 0;
    }

    private void OnBoneSelected(object? sender, EventArgs e)
    {
        if (BonePicker.SelectedItem is string bone)
            BoneSelected?.Invoke(bone);
    }

    private void OnXChanged(object? sender, ValueChangedEventArgs e)
        => RotationXChanged?.Invoke((float)e.NewValue);

    private void OnYChanged(object? sender, ValueChangedEventArgs e)
        => RotationYChanged?.Invoke((float)e.NewValue);

    private void OnZChanged(object? sender, ValueChangedEventArgs e)
        => RotationZChanged?.Invoke((float)e.NewValue);

    public float RotationX
    {
        get => (float)SliderX.Value;
        set => SliderX.Value = value;
    }

    public float RotationY
    {
        get => (float)SliderY.Value;
        set => SliderY.Value = value;
    }

    public float RotationZ
    {
        get => (float)SliderZ.Value;
        set => SliderZ.Value = value;
    }
}
