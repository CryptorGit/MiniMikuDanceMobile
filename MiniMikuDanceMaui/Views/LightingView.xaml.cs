using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui.Views;

public partial class LightingView : ContentView
{
    public event Action<double>? ShadeShiftChanged;
    public event Action<double>? ShadeToonyChanged;
    public event Action<double>? RimIntensityChanged;

    public LightingView()
    {
        InitializeComponent();
    }

    private void OnShadeShiftChanged(object? sender, ValueChangedEventArgs e)
    {
        ShadeShiftValue.Text = $"{e.NewValue:F2}";
        ShadeShiftChanged?.Invoke(e.NewValue);
    }

    private void OnShadeToonyChanged(object? sender, ValueChangedEventArgs e)
    {
        ShadeToonyValue.Text = $"{e.NewValue:F2}";
        ShadeToonyChanged?.Invoke(e.NewValue);
    }

    private void OnRimIntensityChanged(object? sender, ValueChangedEventArgs e)
    {
        RimIntensityValue.Text = $"{e.NewValue:F2}";
        RimIntensityChanged?.Invoke(e.NewValue);
    }

    public double ShadeShift
    {
        get => ShadeShiftSlider.Value;
        set
        {
            ShadeShiftSlider.Value = value;
            ShadeShiftValue.Text = $"{value:F2}";
        }
    }

    public double ShadeToony
    {
        get => ShadeToonySlider.Value;
        set
        {
            ShadeToonySlider.Value = value;
            ShadeToonyValue.Text = $"{value:F2}";
        }
    }

    public double RimIntensity
    {
        get => RimIntensitySlider.Value;
        set
        {
            RimIntensitySlider.Value = value;
            RimIntensityValue.Text = $"{value:F2}";
        }
    }
}
