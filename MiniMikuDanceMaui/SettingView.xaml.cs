using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class SettingView : ContentView
{
    public event Action<double>? HeightRatioChanged;
    public event Action<double>? RotateSensitivityChanged;
    public event Action<double>? PanSensitivityChanged;
    public event Action<double>? StageSizeChanged;
    public event Action<bool>? BoneOutlineChanged;
    public event Action? ResetCameraRequested;

    public SettingView()
    {
        InitializeComponent();
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


    private void OnStageSizeEntryCompleted(object? sender, EventArgs e)
    {
        if (double.TryParse(StageSizeEntry.Text, out var v))
        {
            StageSizeEntry.Text = v.ToString("F1");
            StageSizeChanged?.Invoke(v);
        }
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
        get => double.TryParse(StageSizeEntry.Text, out var v) ? v : 0;
        set => StageSizeEntry.Text = value.ToString("F1");
    }

    public bool ShowBoneOutline
    {
        get => BoneOutlineCheck.IsChecked;
        set => BoneOutlineCheck.IsChecked = value;
    }
}
