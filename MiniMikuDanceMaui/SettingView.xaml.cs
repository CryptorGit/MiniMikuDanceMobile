using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class SettingView : ContentView
{
public event Action<double>? HeightRatioChanged;
    public event Action<double>? SensitivityChanged;

    public SettingView()
    {
        InitializeComponent();
    }

    private void OnHeightChanged(object? sender, ValueChangedEventArgs e)
    {
        HeightRatioChanged?.Invoke(e.NewValue);
    }

    private void OnCameraChanged(object? sender, ValueChangedEventArgs e)
    {
        SensitivityChanged?.Invoke(e.NewValue);
    }

    public double HeightRatio
    {
        get => HeightSlider.Value;
        set => HeightSlider.Value = value;
    }

    public double Sensitivity
    {
        get => CameraSlider.Value;
        set => CameraSlider.Value = value;
    }
}
