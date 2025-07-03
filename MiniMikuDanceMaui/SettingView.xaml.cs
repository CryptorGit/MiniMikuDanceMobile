using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class SettingView : ContentView
{
public event Action<double>? WidthRatioChanged;
    public event Action<double>? SensitivityChanged;

    public SettingView()
    {
        InitializeComponent();
    }

    private void OnWidthChanged(object? sender, ValueChangedEventArgs e)
    {
        WidthRatioChanged?.Invoke(e.NewValue);
    }

    private void OnCameraChanged(object? sender, ValueChangedEventArgs e)
    {
        SensitivityChanged?.Invoke(e.NewValue);
    }

    public double WidthRatio
    {
        get => WidthSlider.Value;
        set => WidthSlider.Value = value;
    }

    public double Sensitivity
    {
        get => CameraSlider.Value;
        set => CameraSlider.Value = value;
    }
}
