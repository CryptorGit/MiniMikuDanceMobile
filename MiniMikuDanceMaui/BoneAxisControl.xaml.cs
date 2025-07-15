using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMikuDanceMaui;

public partial class BoneAxisControl : ContentView
{
    public event Action<double>? ValueChanged;
    public event Action<double>? RangeChanged;
    public event Action? ResetClicked;

    private object? _defaultRange;

    public string ValueFormat { get; set; } = "F0";

    public BoneAxisControl()
    {
        InitializeComponent();
    }

    public void SetPickers(IEnumerable<object> ranges, object defaultRange)
    {
        RangePicker.ItemsSource = ranges.ToList();
        RangePicker.SelectedItem = defaultRange;
        _defaultRange = defaultRange;
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
    }

    public object? SelectedRange
    {
        get => RangePicker.SelectedItem;
        set => RangePicker.SelectedItem = value;
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

    private void OnRangeChanged(object? sender, EventArgs e)
    {
        if (RangePicker.SelectedItem != null && double.TryParse(RangePicker.SelectedItem.ToString(), out var range))
        {
            Slider.Minimum = -range;
            Slider.Maximum = range;
            RangeChanged?.Invoke(range);
        }
    }

    public void Reset()
    {
        if (_defaultRange != null)
            RangePicker.SelectedItem = _defaultRange;
        Slider.Value = 0;
        ResetClicked?.Invoke();
    }

    private void OnResetClicked(object? sender, EventArgs e) => Reset();
}
