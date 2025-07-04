using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class SettingView : ContentView
{
    public event Action<double>? HeightRatioChanged;
    public event Action<double>? SensitivityChanged;
    public event Action<bool>? CameraLockChanged;

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

    private void OnCameraLockChanged(object? sender, CheckedChangedEventArgs e)
    {
        CameraLockChanged?.Invoke(e.Value);
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

    public bool CameraLocked
    {
        get => CameraLockCheck.IsChecked;
        set => CameraLockCheck.IsChecked = value;
    }
}
