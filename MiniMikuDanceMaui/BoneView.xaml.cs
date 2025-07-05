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

    public void SetRotation(OpenTK.Mathematics.Vector3 degrees)
    {
        RotationX = degrees.X;
        RotationY = degrees.Y;
        RotationZ = degrees.Z;
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

    public new float RotationZ
    {
        get => (float)SliderZ.Value;
        set => SliderZ.Value = value;
    }
}
