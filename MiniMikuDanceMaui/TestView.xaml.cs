using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using MiniMikuDance.App;

namespace MiniMikuDanceMaui;

public partial class TestView : ContentView
{
    public event Action<int>? BoneChanged;
    public event Action<string, Vector3>? ParameterChanged;

    private BoneLimit? _rotationLimit;

    public TestView()
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

        RotXControl.ValueChanged += OnParamsChanged;
        RotYControl.ValueChanged += OnParamsChanged;
        RotZControl.ValueChanged += OnParamsChanged;
    }

    public void SetBones(IEnumerable<string> bones)
    {
        var list = bones.ToList();
        BonePicker.ItemsSource = list;
        if (list.Any())
            BonePicker.SelectedIndex = 0;
    }

    public int SelectedBoneIndex
    {
        get => BonePicker.SelectedIndex;
        set => BonePicker.SelectedIndex = value;
    }

    public string SelectedBone => BonePicker.SelectedItem as string ?? string.Empty;

    public Vector3 EulerRotation => new((float)RotXControl.Value, (float)RotYControl.Value, (float)RotZControl.Value);

    public void SetRotationLimit(BoneLimit? limit)
    {
        _rotationLimit = limit;
        RotXControl.SetPickersEnabled(true);
        RotYControl.SetPickersEnabled(true);
        RotZControl.SetPickersEnabled(true);
        RotXControl.SetRange(-180, 180);
        RotYControl.SetRange(-180, 180);
        RotZControl.SetRange(-180, 180);
    }

    public void SetRotation(Vector3 r)
    {
        if (_rotationLimit != null)
        {
            r.X = Math.Clamp(r.X, _rotationLimit.Min.X, _rotationLimit.Max.X);
            r.Y = Math.Clamp(r.Y, _rotationLimit.Min.Y, _rotationLimit.Max.Y);
            r.Z = Math.Clamp(r.Z, _rotationLimit.Min.Z, _rotationLimit.Max.Z);
        }
        RotXControl.Value = r.X;
        RotYControl.Value = r.Y;
        RotZControl.Value = r.Z;
    }

    private void OnBoneChanged(object? sender, EventArgs e)
    {
        BoneChanged?.Invoke(BonePicker.SelectedIndex);
        ParameterChanged?.Invoke(SelectedBone, EulerRotation);
    }

    private void OnParamsChanged(double v)
        => ParameterChanged?.Invoke(SelectedBone, EulerRotation);
}
