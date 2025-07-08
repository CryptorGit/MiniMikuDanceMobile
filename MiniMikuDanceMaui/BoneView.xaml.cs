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

    public BoneView()
    {
        InitializeComponent();

        var rangeValues = Enumerable.Range(0, 37).Select(i => (object)(i * 5)).ToList();
        var centerValues = Enumerable.Range(0, 37).Select(i => (object)(-180 + i * 10)).ToList();
        RotXControl.SetLabels("Rot", "X");
        RotYControl.SetLabels("Rot", "Y");
        RotZControl.SetLabels("Rot", "Z");
        RotXControl.SetPickers(centerValues, rangeValues, 0, 180);
        RotYControl.SetPickers(centerValues, rangeValues, 0, 180);
        RotZControl.SetPickers(centerValues, rangeValues, 0, 180);
        RotXControl.SetRange(-180, 180);
        RotYControl.SetRange(-180, 180);
        RotZControl.SetRange(-180, 180);
        RotXControl.ValueChanged += OnXChanged;
        RotYControl.ValueChanged += OnYChanged;
        RotZControl.ValueChanged += OnZChanged;
        RotXControl.CenterChanged += _ => OnCenterXChanged();
        RotYControl.CenterChanged += _ => OnCenterYChanged();
        RotZControl.CenterChanged += _ => OnCenterZChanged();
        RotXControl.ResetClicked += OnResetClicked;
        RotYControl.ResetClicked += OnResetClicked;
        RotZControl.ResetClicked += OnResetClicked;

        var posRangeValues = Enumerable.Range(0, 11).Select(i => (object)(i / 10f)).ToList();
        var posCenterValues = Enumerable.Range(0, 21).Select(i => (object)(-1f + i * 0.1f)).ToList();
        PosXControl.SetLabels("Pos", "X");
        PosYControl.SetLabels("Pos", "Y");
        PosZControl.SetLabels("Pos", "Z");
        PosXControl.SetPickers(posCenterValues, posRangeValues, 0f, 1f);
        PosYControl.SetPickers(posCenterValues, posRangeValues, 0f, 1f);
        PosZControl.SetPickers(posCenterValues, posRangeValues, 0f, 1f);
        PosXControl.SetRange(-1, 1);
        PosYControl.SetRange(-1, 1);
        PosZControl.SetRange(-1, 1);
        PosXControl.ValueChanged += OnTXChanged;
        PosYControl.ValueChanged += OnTYChanged;
        PosZControl.ValueChanged += OnTZChanged;
        PosXControl.CenterChanged += _ => OnCenterTXChanged();
        PosYControl.CenterChanged += _ => OnCenterTYChanged();
        PosZControl.CenterChanged += _ => OnCenterTZChanged();
        PosXControl.ResetClicked += OnResetClicked;
        PosYControl.ResetClicked += OnResetClicked;
        PosZControl.ResetClicked += OnResetClicked;
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BonePicker.ItemsSource = bones.ToList();
        if (BonePicker.ItemsSource.Cast<object>().Any())
            BonePicker.SelectedIndex = 0;
    }

    public void SetRotation(Vector3 degrees)
    {
        RotXControl.Value = degrees.X;
        RotYControl.Value = degrees.Y;
        RotZControl.Value = degrees.Z;
    }

    public void SetTranslation(Vector3 t)
    {
        PosXControl.Value = t.X;
        PosYControl.Value = t.Y;
        PosZControl.Value = t.Z;
    }

    private void OnBoneSelected(object? sender, EventArgs e)
    {
        if (BonePicker.SelectedIndex >= 0)
            BoneSelected?.Invoke(BonePicker.SelectedIndex);
    }

    private void OnXChanged(double v) => RotationXChanged?.Invoke((float)v);
    private void OnYChanged(double v) => RotationYChanged?.Invoke((float)v);
    private void OnZChanged(double v) => RotationZChanged?.Invoke((float)v);
    private void OnTXChanged(double v) => TranslationXChanged?.Invoke((float)v);
    private void OnTYChanged(double v) => TranslationYChanged?.Invoke((float)v);
    private void OnTZChanged(double v) => TranslationZChanged?.Invoke((float)v);

    private void OnResetClicked() => ResetRequested?.Invoke();

    private void OnCenterXChanged() => RotationXChanged?.Invoke((float)RotXControl.Value);
    private void OnCenterYChanged() => RotationYChanged?.Invoke((float)RotYControl.Value);
    private void OnCenterZChanged() => RotationZChanged?.Invoke((float)RotZControl.Value);
    private void OnCenterTXChanged() => TranslationXChanged?.Invoke((float)PosXControl.Value);
    private void OnCenterTYChanged() => TranslationYChanged?.Invoke((float)PosYControl.Value);
    private void OnCenterTZChanged() => TranslationZChanged?.Invoke((float)PosZControl.Value);

    public new float RotationX
    {
        get => (float)RotXControl.Value;
        set => RotXControl.Value = value;
    }

    public new float RotationY
    {
        get => (float)RotYControl.Value;
        set => RotYControl.Value = value;
    }

    public float RotationZ
    {
        get => (float)RotZControl.Value;
        set => RotZControl.Value = value;
    }

    public new float TranslationX
    {
        get => (float)PosXControl.Value;
        set => PosXControl.Value = value;
    }

    public new float TranslationY
    {
        get => (float)PosYControl.Value;
        set => PosYControl.Value = value;
    }

    public float TranslationZ
    {
        get => (float)PosZControl.Value;
        set => PosZControl.Value = value;
    }

    public void SetRotationRange(int min, int max)
    {
        RotXControl.SetRange(min, max);
        RotYControl.SetRange(min, max);
        RotZControl.SetRange(min, max);
        RotXControl.SelectedRange = Math.Min(max, 180);
        RotYControl.SelectedRange = Math.Min(max, 180);
        RotZControl.SelectedRange = Math.Min(max, 180);
    }

    public void SetTranslationRange(int min, int max)
    {
        PosXControl.SetRange(min, max);
        PosYControl.SetRange(min, max);
        PosZControl.SetRange(min, max);
        PosXControl.SelectedRange = Math.Min(Math.Abs(max), 1f);
        PosYControl.SelectedRange = Math.Min(Math.Abs(max), 1f);
        PosZControl.SelectedRange = Math.Min(Math.Abs(max), 1f);
    }
}
