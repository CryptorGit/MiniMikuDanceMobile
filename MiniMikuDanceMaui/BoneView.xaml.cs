using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace MiniMikuDanceMaui;

public partial class BoneView : ContentView
{
    public event Action<int>? BoneSelected;
    public event Action<float>? RotationXChanged;
    public event Action<float>? RotationYChanged;
    public event Action<float>? RotationZChanged;
    public event Action<float>? TranslationXChanged;
    public event Action<float>? TranslationYChanged;
    public event Action<float>? TranslationZChanged;
    public event Action? ResetRequested;
    public event Action<int>? RotateRangeChanged;
    public event Action<int>? PositionRangeChanged;

    public BoneView()
    {
        InitializeComponent();
        RangeXEntry.Text = "180";
        RangeYEntry.Text = "180";
        RangeZEntry.Text = "180";
        RangeTXEntry.Text = "1";
        RangeTYEntry.Text = "1";
        RangeTZEntry.Text = "1";

        SliderX.Minimum = -180;
        SliderX.Maximum = 180;
        SliderY.Minimum = -180;
        SliderY.Maximum = 180;
        SliderZ.Minimum = -180;
        SliderZ.Maximum = 180;
        SliderTX.Minimum = -1;
        SliderTX.Maximum = 1;
        SliderTY.Minimum = -1;
        SliderTY.Maximum = 1;
        SliderTZ.Minimum = -1;
        SliderTZ.Maximum = 1;
        LabelX.Text = "0";
        LabelY.Text = "0";
        LabelZ.Text = "0";
        LabelTX.Text = "0";
        LabelTY.Text = "0";
        LabelTZ.Text = "0";
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BonePicker.ItemsSource = bones.ToList();
        if (BonePicker.ItemsSource.Cast<object>().Any())
            BonePicker.SelectedIndex = 0;
    }

    public void SetRotation(OpenTK.Mathematics.Vector3 degrees)
    {
        RotationX = degrees.X;
        RotationY = degrees.Y;
        RotationZ = degrees.Z;
        LabelX.Text = $"{degrees.X:F0}";
        LabelY.Text = $"{degrees.Y:F0}";
        LabelZ.Text = $"{degrees.Z:F0}";
    }

    public void SetTranslation(OpenTK.Mathematics.Vector3 t)
    {
        TranslationX = t.X;
        TranslationY = t.Y;
        TranslationZ = t.Z;
        LabelTX.Text = $"{t.X:F2}";
        LabelTY.Text = $"{t.Y:F2}";
        LabelTZ.Text = $"{t.Z:F2}";
    }

    private void OnBoneSelected(object? sender, EventArgs e)
    {
        if (BonePicker.SelectedIndex >= 0)
            BoneSelected?.Invoke(BonePicker.SelectedIndex);
    }

    private void OnXChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelX.Text = $"{e.NewValue:F0}";
        RotationXChanged?.Invoke((float)e.NewValue);
    }

    private void OnYChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelY.Text = $"{e.NewValue:F0}";
        RotationYChanged?.Invoke((float)e.NewValue);
    }

    private void OnZChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelZ.Text = $"{e.NewValue:F0}";
        RotationZChanged?.Invoke((float)e.NewValue);
    }

    private void OnTXChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelTX.Text = $"{e.NewValue:F2}";
        TranslationXChanged?.Invoke((float)e.NewValue);
    }

    private void OnTYChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelTY.Text = $"{e.NewValue:F2}";
        TranslationYChanged?.Invoke((float)e.NewValue);
    }

    private void OnTZChanged(object? sender, ValueChangedEventArgs e)
    {
        LabelTZ.Text = $"{e.NewValue:F2}";
        TranslationZChanged?.Invoke((float)e.NewValue);
    }

    private void OnResetClicked(object? sender, EventArgs e) => ResetRequested?.Invoke();

    private void OnRangeXEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(RangeXEntry.Text, out var v))
        {
            SliderX.Minimum = -v;
            SliderX.Maximum = v;
            RotateRangeChanged?.Invoke((int)v);
        }
    }

    private void OnRangeYEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(RangeYEntry.Text, out var v))
        {
            SliderY.Minimum = -v;
            SliderY.Maximum = v;
            RotateRangeChanged?.Invoke((int)v);
        }
    }

    private void OnRangeZEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(RangeZEntry.Text, out var v))
        {
            SliderZ.Minimum = -v;
            SliderZ.Maximum = v;
            RotateRangeChanged?.Invoke((int)v);
        }
    }

    private void OnRangeTXEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(RangeTXEntry.Text, out var v))
        {
            SliderTX.Minimum = -v;
            SliderTX.Maximum = v;
            PositionRangeChanged?.Invoke((int)v);
        }
    }

    private void OnRangeTYEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(RangeTYEntry.Text, out var v))
        {
            SliderTY.Minimum = -v;
            SliderTY.Maximum = v;
            PositionRangeChanged?.Invoke((int)v);
        }
    }

    private void OnRangeTZEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(RangeTZEntry.Text, out var v))
        {
            SliderTZ.Minimum = -v;
            SliderTZ.Maximum = v;
            PositionRangeChanged?.Invoke((int)v);
        }
    }

    public new float RotationX
    {
        get => (float)SliderX.Value;
        set => SliderX.Value = value;
    }

    public new float RotationY
    {
        get => (float)SliderY.Value;
        set => SliderY.Value = value;
    }

    public float RotationZ
    {
        get => (float)SliderZ.Value;
        set => SliderZ.Value = value;
    }

    public new float TranslationX
    {
        get => (float)SliderTX.Value;
        set => SliderTX.Value = value;
    }

    public new float TranslationY
    {
        get => (float)SliderTY.Value;
        set => SliderTY.Value = value;
    }

    public float TranslationZ
    {
        get => (float)SliderTZ.Value;
        set => SliderTZ.Value = value;
    }

    public void SetRotationRange(int min, int max)
    {
        SliderX.Minimum = min;
        SliderX.Maximum = max;
        SliderY.Minimum = min;
        SliderY.Maximum = max;
        SliderZ.Minimum = min;
        SliderZ.Maximum = max;
        RangeXEntry.Text = Math.Abs(min).ToString();
        RangeYEntry.Text = Math.Abs(min).ToString();
        RangeZEntry.Text = Math.Abs(min).ToString();
    }

    public void SetTranslationRange(int min, int max)
    {
        SliderTX.Minimum = min;
        SliderTX.Maximum = max;
        SliderTY.Minimum = min;
        SliderTY.Maximum = max;
        SliderTZ.Minimum = min;
        SliderTZ.Maximum = max;
        RangeTXEntry.Text = Math.Abs(min).ToString();
        RangeTYEntry.Text = Math.Abs(min).ToString();
        RangeTZEntry.Text = Math.Abs(min).ToString();
    }
}
