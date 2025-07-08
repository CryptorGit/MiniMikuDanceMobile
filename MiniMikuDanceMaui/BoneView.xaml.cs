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

    private float _centerRotX = 0f;
    private float _centerRotY = 0f;
    private float _centerRotZ = 0f;
    private float _centerPosX = 0f;
    private float _centerPosY = 0f;
    private float _centerPosZ = 0f;

    public BoneView()
    {
        InitializeComponent();

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
        var value = (float)(e.NewValue + _centerRotX);
        LabelX.Text = $"{value:F0}";
        RotationXChanged?.Invoke(value);
    }

    private void OnYChanged(object? sender, ValueChangedEventArgs e)
    {
        var value = (float)(e.NewValue + _centerRotY);
        LabelY.Text = $"{value:F0}";
        RotationYChanged?.Invoke(value);
    }

    private void OnZChanged(object? sender, ValueChangedEventArgs e)
    {
        var value = (float)(e.NewValue + _centerRotZ);
        LabelZ.Text = $"{value:F0}";
        RotationZChanged?.Invoke(value);
    }

    private void OnTXChanged(object? sender, ValueChangedEventArgs e)
    {
        var value = (float)(e.NewValue + _centerPosX);
        LabelTX.Text = $"{value:F2}";
        TranslationXChanged?.Invoke(value);
    }

    private void OnTYChanged(object? sender, ValueChangedEventArgs e)
    {
        var value = (float)(e.NewValue + _centerPosY);
        LabelTY.Text = $"{value:F2}";
        TranslationYChanged?.Invoke(value);
    }

    private void OnTZChanged(object? sender, ValueChangedEventArgs e)
    {
        var value = (float)(e.NewValue + _centerPosZ);
        LabelTZ.Text = $"{value:F2}";
        TranslationZChanged?.Invoke(value);
    }

    private void OnResetClicked(object? sender, EventArgs e) => ResetRequested?.Invoke();

    private void OnCenterXEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(CenterXEntry.Text, out var v))
        {
            _centerRotX = v;
            LabelX.Text = $"{SliderX.Value + _centerRotX:F0}";
            RotationXChanged?.Invoke((float)(SliderX.Value + _centerRotX));
        }
    }

    private void OnCenterYEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(CenterYEntry.Text, out var v))
        {
            _centerRotY = v;
            LabelY.Text = $"{SliderY.Value + _centerRotY:F0}";
            RotationYChanged?.Invoke((float)(SliderY.Value + _centerRotY));
        }
    }

    private void OnCenterZEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(CenterZEntry.Text, out var v))
        {
            _centerRotZ = v;
            LabelZ.Text = $"{SliderZ.Value + _centerRotZ:F0}";
            RotationZChanged?.Invoke((float)(SliderZ.Value + _centerRotZ));
        }
    }

    private void OnCenterTXEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(CenterTXEntry.Text, out var v))
        {
            _centerPosX = v;
            LabelTX.Text = $"{SliderTX.Value + _centerPosX:F2}";
            TranslationXChanged?.Invoke((float)(SliderTX.Value + _centerPosX));
        }
    }

    private void OnCenterTYEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(CenterTYEntry.Text, out var v))
        {
            _centerPosY = v;
            LabelTY.Text = $"{SliderTY.Value + _centerPosY:F2}";
            TranslationYChanged?.Invoke((float)(SliderTY.Value + _centerPosY));
        }
    }

    private void OnCenterTZEntryChanged(object? sender, EventArgs e)
    {
        if (float.TryParse(CenterTZEntry.Text, out var v))
        {
            _centerPosZ = v;
            LabelTZ.Text = $"{SliderTZ.Value + _centerPosZ:F2}";
            TranslationZChanged?.Invoke((float)(SliderTZ.Value + _centerPosZ));
        }
    }

    public new float RotationX
    {
        get => (float)(SliderX.Value + _centerRotX);
        set => SliderX.Value = value - _centerRotX;
    }

    public new float RotationY
    {
        get => (float)(SliderY.Value + _centerRotY);
        set => SliderY.Value = value - _centerRotY;
    }

    public float RotationZ
    {
        get => (float)(SliderZ.Value + _centerRotZ);
        set => SliderZ.Value = value - _centerRotZ;
    }

    public new float TranslationX
    {
        get => (float)(SliderTX.Value + _centerPosX);
        set => SliderTX.Value = value - _centerPosX;
    }

    public new float TranslationY
    {
        get => (float)(SliderTY.Value + _centerPosY);
        set => SliderTY.Value = value - _centerPosY;
    }

    public float TranslationZ
    {
        get => (float)(SliderTZ.Value + _centerPosZ);
        set => SliderTZ.Value = value - _centerPosZ;
    }

    public void SetRotationRange(int min, int max)
    {
        SliderX.Minimum = min;
        SliderX.Maximum = max;
        SliderY.Minimum = min;
        SliderY.Maximum = max;
        SliderZ.Minimum = min;
        SliderZ.Maximum = max;
    }

    public void SetTranslationRange(int min, int max)
    {
        SliderTX.Minimum = min;
        SliderTX.Maximum = max;
        SliderTY.Minimum = min;
        SliderTY.Maximum = max;
        SliderTZ.Minimum = min;
        SliderTZ.Maximum = max;
    }
}
