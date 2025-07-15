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
    public event Action? ResetRotationXRequested;
    public event Action? ResetRotationYRequested;
    public event Action? ResetRotationZRequested;
    public event Action? ResetTranslationXRequested;
    public event Action? ResetTranslationYRequested;
    public event Action? ResetTranslationZRequested;

    public BoneView()
    {
        InitializeComponent();

        RotXControl.SetLabels("Rot", "X");
        RotYControl.SetLabels("Rot", "Y");
        RotZControl.SetLabels("Rot", "Z");
        RotXControl.ValueChanged += OnXChanged;
        RotYControl.ValueChanged += OnYChanged;
        RotZControl.ValueChanged += OnZChanged;
        RotXControl.ResetClicked += () => ResetRotationXRequested?.Invoke();
        RotYControl.ResetClicked += () => ResetRotationYRequested?.Invoke();
        RotZControl.ResetClicked += () => ResetRotationZRequested?.Invoke();

        PosXControl.SetLabels("Pos", "X");
        PosYControl.SetLabels("Pos", "Y");
        PosZControl.SetLabels("Pos", "Z");
        PosXControl.SetRange(-1, 1);
        PosYControl.SetRange(-1, 1);
        PosZControl.SetRange(-1, 1);
        PosXControl.ValueChanged += OnTXChanged;
        PosYControl.ValueChanged += OnTYChanged;
        PosZControl.ValueChanged += OnTZChanged;
        PosXControl.ResetClicked += () => ResetTranslationXRequested?.Invoke();
        PosYControl.ResetClicked += () => ResetTranslationYRequested?.Invoke();
        PosZControl.ResetClicked += () => ResetTranslationZRequested?.Invoke();

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

    private void OnResetClicked()
    {
        RotXControl.Reset();
        RotYControl.Reset();
        RotZControl.Reset();
        PosXControl.Reset();
        PosYControl.Reset();
        PosZControl.Reset();
        ResetRequested?.Invoke();
    }

    private void OnResetClicked(object? sender, EventArgs e) => OnResetClicked();


    public float BoneRotationX
    {
        get => (float)RotXControl.Value;
        set => RotXControl.Value = value;
    }

    public float BoneRotationY
    {
        get => (float)RotYControl.Value;
        set => RotYControl.Value = value;
    }

    public float BoneRotationZ
    {
        get => (float)RotZControl.Value;
        set => RotZControl.Value = value;
    }

    public float BoneTranslationX
    {
        get => (float)PosXControl.Value;
        set => PosXControl.Value = value;
    }

    public float BoneTranslationY
    {
        get => (float)PosYControl.Value;
        set => PosYControl.Value = value;
    }

    public float BoneTranslationZ
    {
        get => (float)PosZControl.Value;
        set => PosZControl.Value = value;
    }

    public void SetRotationRange(
        (float Min, float Max) x,
        (float Min, float Max) y,
        (float Min, float Max) z)
    {
        RotXControl.SetRange(x.Min, x.Max);
        RotYControl.SetRange(y.Min, y.Max);
        RotZControl.SetRange(z.Min, z.Max);
    }

    public void SetTranslationRange(int min, int max)
    {
        PosXControl.SetRange(min, max);
        PosYControl.SetRange(min, max);
        PosZControl.SetRange(min, max);
    }


}
