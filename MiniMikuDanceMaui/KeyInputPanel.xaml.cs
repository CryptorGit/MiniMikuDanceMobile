using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace MiniMikuDanceMaui;

public partial class KeyInputPanel : ContentView
{
    public event Action<string, int, Vector3, Vector3>? Confirmed;
    public event Action? Canceled;
    public event Action<int>? BoneChanged;

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            _isEditMode = value;
            ConfirmButton.Text = _isEditMode ? "apply" : "select";
            CancelButton.Text = "cancel";
        }
    }

    public KeyInputPanel()
    {
        InitializeComponent();
        IsEditMode = false;
        PosRangePicker.ItemsSource = new List<int> { 1, 2, 5, 10 };
        PosRangePicker.SelectedItem = 1;
        RotRangePicker.ItemsSource = new List<int> { 30, 45, 90, 180, 360 };
        RotRangePicker.SelectedItem = 180;
        OnPosRangeChanged(null, EventArgs.Empty);
        OnRotRangeChanged(null, EventArgs.Empty);
        UpdateConfirmEnabled();
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BonePicker.ItemsSource = bones.ToList();
        if (BonePicker.ItemsSource.Cast<object>().Any())
            BonePicker.SelectedIndex = 0;
        UpdateConfirmEnabled();
    }

    public void SetFrame(int frame,
        MiniMikuDance.Motion.MotionEditor? editor = null,
        Func<string, int, Vector3>? getTrans = null,
        Func<string, int, Vector3>? getRot = null,
        Func<string, Vector3>? getCurrentTrans = null,
        Func<string, Vector3>? getCurrentRot = null)
    {
        FrameEntry.Text = frame.ToString();

        if (editor == null)
        {
            if (getCurrentTrans != null)
                SetTranslation(getCurrentTrans(SelectedBone));
            if (getCurrentRot != null)
                SetRotation(getCurrentRot(SelectedBone));
            return;
        }

        var (prev, next) = editor.GetNeighborKeyFrames(SelectedBone, frame);
        if (prev == null && next == null)
        {
            if (getCurrentTrans != null)
                SetTranslation(getCurrentTrans(SelectedBone));
            if (getCurrentRot != null)
                SetRotation(getCurrentRot(SelectedBone));
            return;
        }

        Vector3 baseTrans = Vector3.Zero;
        Vector3 baseRot = Vector3.Zero;
        if (prev != null && next != null && getTrans != null && getRot != null)
        {
            var t0 = getTrans(SelectedBone, prev.Value);
            var t1 = getTrans(SelectedBone, next.Value);
            baseTrans = (t0 + t1) * 0.5f;
            var r0 = getRot(SelectedBone, prev.Value);
            var r1 = getRot(SelectedBone, next.Value);
            baseRot = (r0 + r1) * 0.5f;
        }
        else if (prev != null && getTrans != null && getRot != null)
        {
            baseTrans = getTrans(SelectedBone, prev.Value);
            baseRot = getRot(SelectedBone, prev.Value);
        }
        else if (next != null && getTrans != null && getRot != null)
        {
            baseTrans = getTrans(SelectedBone, next.Value);
            baseRot = getRot(SelectedBone, next.Value);
        }
        else
        {
            if (getCurrentTrans != null)
                baseTrans = getCurrentTrans(SelectedBone);
            if (getCurrentRot != null)
                baseRot = getCurrentRot(SelectedBone);
        }

        SetTranslation(baseTrans);
        SetRotation(baseRot);
    }

    public int FrameNumber => int.TryParse(FrameEntry.Text, out var f) ? f : 0;
    public string SelectedBone => BonePicker.SelectedItem as string ?? string.Empty;
    public int SelectedBoneIndex => BonePicker.SelectedIndex;

    public Vector3 Translation => new((float)PosXSlider.Value, (float)PosYSlider.Value, (float)PosZSlider.Value);
    public Vector3 EulerRotation => new((float)RotXSlider.Value, (float)RotYSlider.Value, (float)RotZSlider.Value);

    public void SetTranslation(Vector3 t)
    {
        PosXSlider.Value = t.X; PosYSlider.Value = t.Y; PosZSlider.Value = t.Z;
        PosXLabel.Text = $"{t.X:F2}"; PosYLabel.Text = $"{t.Y:F2}"; PosZLabel.Text = $"{t.Z:F2}";
    }

    public void SetRotation(Vector3 r)
    {
        RotXSlider.Value = r.X; RotYSlider.Value = r.Y; RotZSlider.Value = r.Z;
        RotXLabel.Text = $"{r.X:F0}"; RotYLabel.Text = $"{r.Y:F0}"; RotZLabel.Text = $"{r.Z:F0}";
    }

    private void OnConfirmClicked(object? sender, EventArgs e)
        => Confirmed?.Invoke(SelectedBone, FrameNumber, Translation, EulerRotation);

    private void OnCancelClicked(object? sender, EventArgs e)
        => Canceled?.Invoke();

    private void OnFrameMinusClicked(object? sender, EventArgs e)
    {
        if (int.TryParse(FrameEntry.Text, out var value))
            FrameEntry.Text = (value - 1).ToString();
        else
            FrameEntry.Text = "0";
        UpdateConfirmEnabled();
    }

    private void OnFramePlusClicked(object? sender, EventArgs e)
    {
        if (int.TryParse(FrameEntry.Text, out var value))
            FrameEntry.Text = (value + 1).ToString();
        else
            FrameEntry.Text = "0";
        UpdateConfirmEnabled();
    }

    private void OnPosXSetClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && double.TryParse(btn.Text, out var v))
            PosXSlider.Value = v;
    }

    private void OnPosYSetClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && double.TryParse(btn.Text, out var v))
            PosYSlider.Value = v;
    }

    private void OnPosZSetClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && double.TryParse(btn.Text, out var v))
            PosZSlider.Value = v;
    }

    private void OnRotXSetClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && double.TryParse(btn.Text, out var v))
            RotXSlider.Value = v;
    }

    private void OnRotYSetClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && double.TryParse(btn.Text, out var v))
            RotYSlider.Value = v;
    }

    private void OnRotZSetClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && double.TryParse(btn.Text, out var v))
            RotZSlider.Value = v;
    }

    private void UpdateConfirmEnabled()
        => ConfirmButton.IsEnabled = BonePicker.SelectedIndex >= 0 && int.TryParse(FrameEntry.Text, out _);

    private void OnFrameTextChanged(object? sender, TextChangedEventArgs e)
        => UpdateConfirmEnabled();

    private void OnRotRangeChanged(object? sender, EventArgs e)
    {
        if (RotRangePicker.SelectedItem is int range)
        {
            RotXSlider.Minimum = -range; RotXSlider.Maximum = range;
            RotYSlider.Minimum = -range; RotYSlider.Maximum = range;
            RotZSlider.Minimum = -range; RotZSlider.Maximum = range;
        }
    }

    private void OnPosRangeChanged(object? sender, EventArgs e)
    {
        if (PosRangePicker.SelectedItem is int range)
        {
            PosXSlider.Minimum = -range; PosXSlider.Maximum = range;
            PosYSlider.Minimum = -range; PosYSlider.Maximum = range;
            PosZSlider.Minimum = -range; PosZSlider.Maximum = range;
        }
    }

    private void OnPosXChanged(object? sender, ValueChangedEventArgs e)
        => PosXLabel.Text = $"{e.NewValue:F2}";
    private void OnPosYChanged(object? sender, ValueChangedEventArgs e)
        => PosYLabel.Text = $"{e.NewValue:F2}";
    private void OnPosZChanged(object? sender, ValueChangedEventArgs e)
        => PosZLabel.Text = $"{e.NewValue:F2}";
    private void OnRotXChanged(object? sender, ValueChangedEventArgs e)
        => RotXLabel.Text = $"{e.NewValue:F0}";
    private void OnRotYChanged(object? sender, ValueChangedEventArgs e)
        => RotYLabel.Text = $"{e.NewValue:F0}";
    private void OnRotZChanged(object? sender, ValueChangedEventArgs e)
        => RotZLabel.Text = $"{e.NewValue:F0}";

    private void OnBoneChanged(object? sender, EventArgs e)
    {
        BoneChanged?.Invoke(BonePicker.SelectedIndex);
        UpdateConfirmEnabled();
    }
}
