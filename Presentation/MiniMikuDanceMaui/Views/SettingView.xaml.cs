using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class SettingView : ContentView
{
    public event Action<double>? HeightRatioChanged;
    public event Action<double>? RotateSensitivityChanged;
    public event Action<double>? PanSensitivityChanged;
    public event Action<double>? ZoomSensitivityChanged;
    public event Action<double>? IkBoneSizeChanged;
    public event Action<double>? StageSizeChanged;
    public event Action<double>? BonePickPixelsChanged;
    public event Action<bool>? BoneOutlineChanged;
    public event Action<bool>? BoneTypeChanged;
    public event Action<bool>? LockTranslationChanged;
    public event Action? ResetCameraRequested;

    public SettingView()
    {
        InitializeComponent();
    }

    private void HandleSliderChange(Action<double>? callback, ValueChangedEventArgs e)
    {
        callback?.Invoke(e.NewValue);
    }

    private void HandleCheckChange(Action<bool>? callback, CheckedChangedEventArgs e)
    {
        callback?.Invoke(e.Value);
    }

    private void OnHeightChanged(object? sender, ValueChangedEventArgs e)
    {
        HandleSliderChange(HeightRatioChanged, e);
    }

    private void OnRotateChanged(object? sender, ValueChangedEventArgs e)
    {
        HandleSliderChange(RotateSensitivityChanged, e);
    }

    private void OnPanChanged(object? sender, ValueChangedEventArgs e)
    {
        HandleSliderChange(PanSensitivityChanged, e);
    }

    private void OnZoomChanged(object? sender, ValueChangedEventArgs e)
    {
        HandleSliderChange(ZoomSensitivityChanged, e);
    }

    private void OnIkBoneSizeChanged(object? sender, ValueChangedEventArgs e)
    {
        HandleSliderChange(IkBoneSizeChanged, e);
    }

    private void OnBonePickPixelsChanged(object? sender, ValueChangedEventArgs e)
    {
        HandleSliderChange(BonePickPixelsChanged, e);
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
        HandleCheckChange(BoneOutlineChanged, e);
    }

    private void OnBoneTypeChanged(object? sender, CheckedChangedEventArgs e)
    {
        HandleCheckChange(BoneTypeChanged, e);
    }

    private void OnLockTranslationChanged(object? sender, CheckedChangedEventArgs e)
    {
        HandleCheckChange(LockTranslationChanged, e);
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

    public double ZoomSensitivity
    {
        get => ZoomSlider.Value;
        set => ZoomSlider.Value = value;
    }

    public double IkBoneSize
    {
        get => IkBoneSizeSlider.Value;
        set => IkBoneSizeSlider.Value = value;
    }

    public double BonePickPixels
    {
        get => BonePickSlider.Value;
        set => BonePickSlider.Value = value;
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

    public bool DistinguishBoneTypes
    {
        get => BoneTypeCheck.IsChecked;
        set => BoneTypeCheck.IsChecked = value;
    }

    public bool LockTranslation
    {
        get => LockTranslationCheck.IsChecked;
        set => LockTranslationCheck.IsChecked = value;
    }
}
