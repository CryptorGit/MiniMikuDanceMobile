using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class MToonView : ContentView
{
    public event Action<double>? ShadeShiftChanged;
    public event Action<double>? ShadeToonyChanged;
    public event Action<double>? RimIntensityChanged;

    public MToonView()
    {
        InitializeComponent();
    }

    private void OnShadeShiftChanged(object? sender, ValueChangedEventArgs e)
    {
        ShadeShiftChanged?.Invoke(e.NewValue);
    }

    private void OnShadeToonyChanged(object? sender, ValueChangedEventArgs e)
    {
        ShadeToonyChanged?.Invoke(e.NewValue);
    }

    private void OnRimIntensityChanged(object? sender, ValueChangedEventArgs e)
    {
        RimIntensityChanged?.Invoke(e.NewValue);
    }

    public double ShadeShift
    {
        get => ShadeShiftSlider.Value;
        set => ShadeShiftSlider.Value = value;
    }

    public double ShadeToony
    {
        get => ShadeToonySlider.Value;
        set => ShadeToonySlider.Value = value;
    }

    public double RimIntensity
    {
        get => RimIntensitySlider.Value;
        set => RimIntensitySlider.Value = value;
    }
}
