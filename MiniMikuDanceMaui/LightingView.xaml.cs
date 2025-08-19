using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class LightingView : ContentView
{
    public event Action<double>? ShadeShiftChanged;
    public event Action<double>? ShadeToonyChanged;
    public event Action<double>? RimIntensityChanged;
    public event Action<double>? SphereStrengthChanged;
    public event Action<double>? ToonStrengthChanged;

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

    private void OnSphereStrengthChanged(object? sender, ValueChangedEventArgs e)
    {
        SphereStrengthValue.Text = $"{e.NewValue:F2}";
        SphereStrengthChanged?.Invoke(e.NewValue);
    }

    private void OnToonStrengthChanged(object? sender, ValueChangedEventArgs e)
    {
        ToonStrengthValue.Text = $"{e.NewValue:F2}";
        ToonStrengthChanged?.Invoke(e.NewValue);
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

    public double SphereStrength
    {
        get => SphereStrengthSlider.Value;
        set
        {
            SphereStrengthSlider.Value = value;
            SphereStrengthValue.Text = $"{value:F2}";
        }
    }

    public double ToonStrength
    {
        get => ToonStrengthSlider.Value;
        set
        {
            ToonStrengthSlider.Value = value;
            ToonStrengthValue.Text = $"{value:F2}";
        }
    }
}
