using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class SettingView : ContentView
{
    public event Action<double>? HeightRatioChanged;
    public event Action<double>? RotateSensitivityChanged;
    public event Action<double>? PanSensitivityChanged;
    public event Action<bool>? CameraLockChanged;

    public SettingView()
    {
        InitializeComponent();
    }

    private void OnHeightChanged(object? sender, ValueChangedEventArgs e)
    {
        HeightRatioChanged?.Invoke(e.NewValue);
    }

    private void OnRotateChanged(object? sender, ValueChangedEventArgs e)
    {
        RotateSensitivityChanged?.Invoke(e.NewValue);
    }

    private void OnPanChanged(object? sender, ValueChangedEventArgs e)
    {
        PanSensitivityChanged?.Invoke(e.NewValue);
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

    public double RotateSensitivity
    {
        get => RotateSlider.Value;
        set => RotateSlider.Value = value;
    }

    public double PanSensitivity
    {
        get => PanSlider.Value;
        set => PanSlider.Value = value;
    }

    public bool CameraLocked
    {
        get => CameraLockCheck.IsChecked;
        set => CameraLockCheck.IsChecked = value;
    }
}
