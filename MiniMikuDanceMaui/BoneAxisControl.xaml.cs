using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMikuDanceMaui;

public partial class BoneAxisControl : ContentView
{
    public event Action<double>? ValueChanged;
    public event Action<double>? CenterChanged;
    public event Action<double>? RangeChanged;
    public event Action? ResetClicked;

    private double _center = 0;
    private object? _defaultCenter;
    private object? _defaultRange;

    public string ValueFormat { get; set; } = "F0";

    public BoneAxisControl()
    {
        InitializeComponent();
    }

    public void SetPickers(IEnumerable<object> centers, IEnumerable<object> ranges, object defaultCenter, object defaultRange)
    {
        CenterPicker.ItemsSource = centers.ToList();
        RangePicker.ItemsSource = ranges.ToList();
        CenterPicker.SelectedItem = defaultCenter;
        RangePicker.SelectedItem = defaultRange;
        _defaultCenter = defaultCenter;
        _defaultRange = defaultRange;
    }

    public double Value
    {
        get => Slider.Value + _center;
        set
        {
            Slider.Value = value - _center;
            ValueLabel.Text = (Slider.Value + _center).ToString(ValueFormat);
        }
    }

    public double Center => _center;

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

    public object? SelectedCenter
    {
        get => CenterPicker.SelectedItem;
        set => CenterPicker.SelectedItem = value;
    }

    public void SetLabels(string type, string axis)
    {
        TypeLabel.Text = type;
        AxisLabel.Text = axis;
    }

    private void OnSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        ValueLabel.Text = (e.NewValue + _center).ToString(ValueFormat);
        ValueChanged?.Invoke(e.NewValue + _center);
    }

    private void OnCenterChanged(object? sender, EventArgs e)
    {
        if (CenterPicker.SelectedItem != null && double.TryParse(CenterPicker.SelectedItem.ToString(), out var v))
        {
            _center = v;
            ValueLabel.Text = (Slider.Value + _center).ToString(ValueFormat);
            CenterChanged?.Invoke(_center);
            ValueChanged?.Invoke(Slider.Value + _center);
        }
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
        if (_defaultCenter != null)
            CenterPicker.SelectedItem = _defaultCenter;
        if (_defaultRange != null)
            RangePicker.SelectedItem = _defaultRange;
        Slider.Value = 0;
        ResetClicked?.Invoke();
    }

    private void OnResetClicked(object? sender, EventArgs e) => Reset();
}
