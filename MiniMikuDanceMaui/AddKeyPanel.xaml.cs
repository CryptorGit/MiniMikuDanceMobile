using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace MiniMikuDanceMaui;

public partial class AddKeyPanel : ContentView
{
public event Action<string, int, Vector3, Vector3>? Confirmed;
public event Action? Canceled;
public event Action<int>? BoneChanged;
public event Action<int>? FrameChanged;

private Func<string, int, Vector3>? _getTranslation;
private Func<string, int, Vector3>? _getRotation;

    public AddKeyPanel()
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

    public void SetFrame(int frame, IEnumerable<int> usedFrames,
                         Func<string, int, Vector3>? getTranslation = null,
                         Func<string, int, Vector3>? getRotation = null)
    {
        _getTranslation = getTranslation;
        _getRotation = getRotation;

        TitleLabel.Text = "Add Keyframe";
        FrameEntryGrid.IsVisible = true;
        var list = Enumerable.Range(0, TimelineView.MaxFrame)
            .Where(f => !usedFrames.Contains(f))
            .ToList();
        FrameEntryPicker.ItemsSource = list;
        FrameEntryPicker.SelectedItem = list.Contains(frame) ? frame : (list.Count > 0 ? list[0] : 0);
    }

    public int FrameNumber
        => FrameEntryPicker.SelectedItem is int f ? f : 0;
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
        var applyButton = this.FindByName<Button>("ApplyButton");
        if (applyButton != null)
        {
            bool frameValid = FrameEntryPicker.SelectedIndex >= 0;
            applyButton.IsEnabled = BonePicker.SelectedIndex >= 0 && frameValid;
        }
    }

    private void OnFramePickerChanged(object? sender, EventArgs e)
    {
        UpdateConfirmEnabled();
        FrameChanged?.Invoke(FrameNumber);
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
    }

}
