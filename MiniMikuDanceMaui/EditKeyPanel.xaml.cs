using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using MiniMikuDance.App;

namespace MiniMikuDanceMaui;

public partial class EditKeyPanel : ContentView
{
public event Action<string, int, Vector3, Vector3>? Confirmed;
public event Action? Canceled;
public event Action<int>? BoneChanged;
public event Action<int>? FrameChanged;
public event Action<string, int, Vector3, Vector3>? ParameterChanged;

private Func<string, int, Vector3>? _getTranslation;
private Func<string, int, Vector3>? _getRotation;
    private MiniMikuDance.App.BoneLimit? _rotationLimit;

    public EditKeyPanel()
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
        UpdateConfirmEnabled();

        RotXControl.ValueChanged += OnParamsChanged;
        RotYControl.ValueChanged += OnParamsChanged;
        RotZControl.ValueChanged += OnParamsChanged;
        PosXControl.ValueChanged += OnParamsChanged;
        PosYControl.ValueChanged += OnParamsChanged;
        PosZControl.ValueChanged += OnParamsChanged;
    }

    public void SetRotationLimit(MiniMikuDance.App.BoneLimit? limit)
    {
        _rotationLimit = limit;
        // 回転制約があってもピッカーとリセットを無効化しない
        RotXControl.SetPickersEnabled(true);
        RotYControl.SetPickersEnabled(true);
        RotZControl.SetPickersEnabled(true);

        // スライダー範囲は固定し、値は後で丸め込む
        RotXControl.SetRange(-180, 180);
        RotYControl.SetRange(-180, 180);
        RotZControl.SetRange(-180, 180);
    }

    public void SetBones(IEnumerable<string> bones)
    {
        var boneList = bones.ToList();
        BonePicker.ItemsSource = boneList;
        if (boneList.Any())
            BonePicker.SelectedIndex = 0;
        UpdateConfirmEnabled();
    }

    public void SetFrame(int frame, IEnumerable<int> frames,
                         Func<string, int, Vector3> getTranslation, Func<string, int, Vector3> getRotation)
    {
        _getTranslation = getTranslation;
        _getRotation = getRotation;
        TitleLabel.Text = "Edit Keyframe";
        FrameEntryGrid.IsVisible = false;
        FramePickerGrid.IsVisible = true;
        FramePicker.ItemsSource = frames.ToList();
        FramePicker.SelectedItem = frame;

        if (getTranslation != null && getRotation != null)
        {
            var boneName = SelectedBone;
            if (!string.IsNullOrEmpty(boneName))
            {
                SetTranslation(getTranslation(boneName, frame));
                SetRotation(getRotation(boneName, frame));
            }
        }
    }

    public void SetFrameOptions(IEnumerable<int> frames)
    {
        if (!FramePickerGrid.IsVisible)
            return;
        var list = frames.ToList();
        var currentFrame = FrameNumber;
        FramePicker.ItemsSource = list;
        if (list.Contains(currentFrame))
        {
            FramePicker.SelectedItem = currentFrame;
        }
        else if (list.Count > 0)
        {
            FramePicker.SelectedIndex = 0;
        }
        UpdateConfirmEnabled();
    }

    public int FrameNumber
        => FramePickerGrid.IsVisible && FramePicker.SelectedItem is int f1
            ? f1
            : (FrameEntryPicker.SelectedItem is int f2 ? f2 : 0);
    public int SelectedBoneIndex
    {
        get => BonePicker.SelectedIndex;
        set => BonePicker.SelectedIndex = value;
    }
    public string SelectedBone => BonePicker.SelectedItem as string ?? string.Empty;

    public Vector3 Translation => new((float)PosXControl.Value, (float)PosYControl.Value, (float)PosZControl.Value);
    public Vector3 EulerRotation => new((float)RotXControl.Value, (float)RotYControl.Value, (float)RotZControl.Value);

    public void SetTranslation(Vector3 t)
    {
        PosXControl.Value = t.X;
        PosYControl.Value = t.Y;
        PosZControl.Value = t.Z;
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

    private void OnConfirmClicked(object? sender, EventArgs e)
        => Confirmed?.Invoke(SelectedBone, FrameNumber, Translation, EulerRotation);

    private void OnCancelClicked(object? sender, EventArgs e)
        => Canceled?.Invoke();

    private void UpdateConfirmEnabled()
    {
        // Assuming the Apply button is named ApplyButton in XAML
        var applyButton = this.FindByName<Button>("ApplyButton");
        if (applyButton != null)
        {
            bool frameValid = FramePickerGrid.IsVisible
                ? FramePicker.SelectedIndex >= 0
                : FrameEntryPicker.SelectedIndex >= 0;
            applyButton.IsEnabled = BonePicker.SelectedIndex >= 0 && frameValid;
        }
    }

    private void OnFramePickerChanged(object? sender, EventArgs e)
    {
        UpdateConfirmEnabled();
        if (_getTranslation != null && _getRotation != null && FramePicker.SelectedItem is int f)
        {
            var boneName = SelectedBone;
            if (!string.IsNullOrEmpty(boneName))
            {
                SetTranslation(_getTranslation(boneName, f));
                SetRotation(_getRotation(boneName, f));
            }
        }
        FrameChanged?.Invoke(FrameNumber);
        ParameterChanged?.Invoke(SelectedBone, FrameNumber, Translation, EulerRotation);
    }

    private void OnBoneChanged(object? sender, EventArgs e)
    {
        BoneChanged?.Invoke(BonePicker.SelectedIndex);
        UpdateConfirmEnabled();

        if (_getTranslation != null && _getRotation != null)
        {
            var boneName = SelectedBone;
            int frame = FrameNumber;
            if (!string.IsNullOrEmpty(boneName))
            {
                SetTranslation(_getTranslation(boneName, frame));
                SetRotation(_getRotation(boneName, frame));
            }
        }
        ParameterChanged?.Invoke(SelectedBone, FrameNumber, Translation, EulerRotation);
    }

    private void OnParamsChanged(double v)
        => ParameterChanged?.Invoke(SelectedBone, FrameNumber, Translation, EulerRotation);

}
