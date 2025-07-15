using Microsoft.Maui.Controls;
using System;

namespace MiniMikuDanceMaui;

public partial class BoneAxisControl : ContentView
{
    public event Action<double>? ValueChanged;
    public event Action? ResetClicked;

    public string ValueFormat { get; set; } = "F0";

    public BoneAxisControl()
    {
        InitializeComponent();
    }


    public double Value
    {
        get => Slider.Value;
        set
        {
            Slider.Value = value;
            ValueLabel.Text = Slider.Value.ToString(ValueFormat);
        }
    }


    public void SetRange(double min, double max)
    {
        Slider.Minimum = min;
        Slider.Maximum = max;
        MinLabel.Text = min.ToString(ValueFormat);
        MaxLabel.Text = max.ToString(ValueFormat);
    }

    public void SetLabels(string type, string axis)
    {
        TypeLabel.Text = type;
        AxisLabel.Text = axis;
    }

    private void OnSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        ValueLabel.Text = e.NewValue.ToString(ValueFormat);
        ValueChanged?.Invoke(e.NewValue);
    }

    public void Reset()
    {
        Slider.Value = 0;
        ResetClicked?.Invoke();
    }

    private void OnResetClicked(object? sender, EventArgs e) => Reset();
}
