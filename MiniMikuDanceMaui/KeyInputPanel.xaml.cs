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
public event Action<int>? FrameChanged;

private bool _isEditMode;
private Func<string, int, Vector3>? _getTranslation;
private Func<string, int, Vector3>? _getRotation;

    public KeyInputPanel()
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
    }

    public void SetBones(IEnumerable<string> bones)
    {
        var boneList = bones.ToList();
        BonePicker.ItemsSource = boneList;
        if (boneList.Any())
            BonePicker.SelectedIndex = 0;
        UpdateConfirmEnabled();
    }

    public void SetFrame(int frame, bool isEditMode = false, IEnumerable<int>? frames = null,
                         Func<string, int, Vector3>? getTranslation = null, Func<string, int, Vector3>? getRotation = null)
    {
        _isEditMode = isEditMode;
        _getTranslation = getTranslation;
        _getRotation = getRotation;
        if (isEditMode)
        {
            FrameEntryGrid.IsVisible = false;
            FramePickerGrid.IsVisible = true;
            if (frames != null)
            {
                FramePicker.ItemsSource = frames.ToList();
                FramePicker.SelectedItem = frame;
            }
        }
        else
        {
            FrameEntryGrid.IsVisible = true;
            FramePickerGrid.IsVisible = false;
            FrameEntry.Text = frame.ToString();
        }

        if (isEditMode && getTranslation != null && getRotation != null)
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
        FramePicker.ItemsSource = list;
        if (list.Count > 0)
            FramePicker.SelectedIndex = 0;
        UpdateConfirmEnabled();
    }

    public int FrameNumber
        => FramePickerGrid.IsVisible && FramePicker.SelectedItem is int f1
            ? f1
            : (int.TryParse(FrameEntry.Text, out var f2) ? f2 : 0);
    public int SelectedBoneIndex => BonePicker.SelectedIndex;
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
                : int.TryParse(FrameEntry.Text, out _);
            applyButton.IsEnabled = BonePicker.SelectedIndex >= 0 && frameValid;
        }
    }

    private void OnFrameTextChanged(object? sender, TextChangedEventArgs e)
        => UpdateConfirmEnabled();

    private void OnFramePickerChanged(object? sender, EventArgs e)
    {
        UpdateConfirmEnabled();
        if (_isEditMode && _getTranslation != null && _getRotation != null && FramePicker.SelectedItem is int f)
        {
            var boneName = SelectedBone;
            if (!string.IsNullOrEmpty(boneName))
            {
                SetTranslation(_getTranslation(boneName, f));
                SetRotation(_getRotation(boneName, f));
            }
        }
        FrameChanged?.Invoke(FrameNumber);
    }

    private void OnBoneChanged(object? sender, EventArgs e)
    {
        BoneChanged?.Invoke(BonePicker.SelectedIndex);
        UpdateConfirmEnabled();

        if (_isEditMode && _getTranslation != null && _getRotation != null)
        {
            var boneName = SelectedBone;
            int frame = FrameNumber;
            if (!string.IsNullOrEmpty(boneName))
            {
                SetTranslation(_getTranslation(boneName, frame));
                SetRotation(_getRotation(boneName, frame));
            }
        }
    }

}
