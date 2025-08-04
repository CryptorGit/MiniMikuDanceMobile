using Microsoft.Maui.Controls;
using System;
using MiniMikuDance.App;

namespace MiniMikuDanceMaui;

public partial class SettingView : ContentView
{
    public event Action<double>? HeightRatioChanged;
    public event Action<double>? RotateSensitivityChanged;
    public event Action<double>? PanSensitivityChanged;
    public event Action<double>? StageSizeChanged;
    public event Action<bool>? CameraLockChanged;
    public event Action<bool>? BoneOutlineChanged;
    public event Action? ResetCameraRequested;

    public SettingView()
    {
        InitializeComponent();
        StageSizePicker.SelectedItem = AppSettings.DefaultStageSize.ToString("F0");
    }

    private void OnHeightChanged(object? sender, ValueChangedEventArgs e)
    {
        LogService.WriteLine($"Height slider: {e.NewValue:F2}");
        HeightRatioChanged?.Invoke(e.NewValue);
    }

    private void OnRotateChanged(object? sender, ValueChangedEventArgs e)
    {
        LogService.WriteLine($"Rotate sensitivity: {e.NewValue:F2}");
        RotateSensitivityChanged?.Invoke(e.NewValue);
    }

    private void OnPanChanged(object? sender, ValueChangedEventArgs e)
    {
        LogService.WriteLine($"Pan sensitivity: {e.NewValue:F2}");
        PanSensitivityChanged?.Invoke(e.NewValue);
    }

    private void OnStageSizePickerChanged(object? sender, EventArgs e)
    {
        if (StageSizePicker.SelectedItem is string s && double.TryParse(s, out var v))
        {
            LogService.WriteLine($"Stage size: {v:F1}");
            StageSizeChanged?.Invoke(v);
        }
    }

    private void OnCameraLockChanged(object? sender, CheckedChangedEventArgs e)
    {
        LogService.WriteLine($"Camera lock: {e.Value}");
        CameraLockChanged?.Invoke(e.Value);
    }

    private void OnBoneOutlineChanged(object? sender, CheckedChangedEventArgs e)
    {
        LogService.WriteLine($"Bone outline: {e.Value}");
        BoneOutlineChanged?.Invoke(e.Value);
    }

    private void OnResetCameraClicked(object? sender, EventArgs e)
    {
        ResetCameraRequested?.Invoke();
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

    public double StageSize
    {
        get
        {
            if (StageSizePicker.SelectedItem is string s && double.TryParse(s, out var v))
            {
                return v;
            }
            return AppSettings.DefaultStageSize;
        }
        set => StageSizePicker.SelectedItem = value.ToString("F0");
    }

    public bool CameraLocked
    {
        get => CameraLockCheck.IsChecked;
        set => CameraLockCheck.IsChecked = value;
    }

    public bool ShowBoneOutline
    {
        get => BoneOutlineCheck.IsChecked;
        set => BoneOutlineCheck.IsChecked = value;
    }
}
