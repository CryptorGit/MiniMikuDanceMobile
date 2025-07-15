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
    public event Action<Vector3>? SolveIkRequested;
    public event Action<(float Min, float Max)>? RotationXLimitChanged;
    public event Action<(float Min, float Max)>? RotationYLimitChanged;
    public event Action<(float Min, float Max)>? RotationZLimitChanged;

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
        RotXControl.CenterChanged += OnCenterXChanged;
        RotYControl.CenterChanged += OnCenterYChanged;
        RotZControl.CenterChanged += OnCenterZChanged;
        RotXControl.ResetClicked += () => ResetRotationXRequested?.Invoke();
        RotYControl.ResetClicked += () => ResetRotationYRequested?.Invoke();
        RotZControl.ResetClicked += () => ResetRotationZRequested?.Invoke();

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
        PosXControl.CenterChanged += OnCenterTXChanged;
        PosYControl.CenterChanged += OnCenterTYChanged;
        PosZControl.CenterChanged += OnCenterTZChanged;
        PosXControl.ResetClicked += () => ResetTranslationXRequested?.Invoke();
        PosYControl.ResetClicked += () => ResetTranslationYRequested?.Invoke();
        PosZControl.ResetClicked += () => ResetTranslationZRequested?.Invoke();

        IkBonePicker.SelectedIndexChanged += OnIkBoneChanged;
        IkXEntry.Text = "0";
        IkYEntry.Text = "0";
        IkZEntry.Text = "0";
        RotXMinEntry.TextChanged += OnRotXLimitChanged;
        RotXMaxEntry.TextChanged += OnRotXLimitChanged;
        RotYMinEntry.TextChanged += OnRotYLimitChanged;
        RotYMaxEntry.TextChanged += OnRotYLimitChanged;
        RotZMinEntry.TextChanged += OnRotZLimitChanged;
        RotZMaxEntry.TextChanged += OnRotZLimitChanged;
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BonePicker.ItemsSource = bones.ToList();
        if (BonePicker.ItemsSource.Cast<object>().Any())
            BonePicker.SelectedIndex = 0;
        IkBonePicker.ItemsSource = bones.ToList();
        if (IkBonePicker.ItemsSource.Cast<object>().Any())
            IkBonePicker.SelectedIndex = 0;
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

    private void OnIkBoneChanged(object? sender, EventArgs e)
    {
        // no action required currently
    }

    private void OnSolveIkClicked(object? sender, EventArgs e)
    {
        if (float.TryParse(IkXEntry.Text, out var x) &&
            float.TryParse(IkYEntry.Text, out var y) &&
            float.TryParse(IkZEntry.Text, out var z))
        {
            SolveIkRequested?.Invoke(new Vector3(x, y, z));
        }
    }

    private void OnRotXLimitChanged(object? sender, TextChangedEventArgs e)
    {
        if (float.TryParse(RotXMinEntry.Text, out var min) && float.TryParse(RotXMaxEntry.Text, out var max))
            RotationXLimitChanged?.Invoke((min, max));
    }

    private void OnRotYLimitChanged(object? sender, TextChangedEventArgs e)
    {
        if (float.TryParse(RotYMinEntry.Text, out var min) && float.TryParse(RotYMaxEntry.Text, out var max))
            RotationYLimitChanged?.Invoke((min, max));
    }

    private void OnRotZLimitChanged(object? sender, TextChangedEventArgs e)
    {
        if (float.TryParse(RotZMinEntry.Text, out var min) && float.TryParse(RotZMaxEntry.Text, out var max))
            RotationZLimitChanged?.Invoke((min, max));
    }

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

    private void OnCenterXChanged(double _) => RotationXChanged?.Invoke((float)RotXControl.Value);
    private void OnCenterYChanged(double _) => RotationYChanged?.Invoke((float)RotYControl.Value);
    private void OnCenterZChanged(double _) => RotationZChanged?.Invoke((float)RotZControl.Value);
    private void OnCenterTXChanged(double _) => TranslationXChanged?.Invoke((float)PosXControl.Value);
    private void OnCenterTYChanged(double _) => TranslationYChanged?.Invoke((float)PosYControl.Value);
    private void OnCenterTZChanged(double _) => TranslationZChanged?.Invoke((float)PosZControl.Value);

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

    public float RotationZ
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

    public int SelectedIkTargetBoneIndex => IkBonePicker.SelectedIndex;

    public void SetRotationLimits((float Min, float Max) x, (float Min, float Max) y, (float Min, float Max) z)
    {
        RotXMinEntry.Text = x.Min.ToString();
        RotXMaxEntry.Text = x.Max.ToString();
        RotYMinEntry.Text = y.Min.ToString();
        RotYMaxEntry.Text = y.Max.ToString();
        RotZMinEntry.Text = z.Min.ToString();
        RotZMaxEntry.Text = z.Max.ToString();
    }
}
