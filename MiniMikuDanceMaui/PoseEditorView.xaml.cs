using Microsoft.Maui.Controls;
using MiniMikuDance.Import;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MiniMikuDanceMaui;

public partial class PoseEditorView : ContentView
{
    private IReadOnlyList<BoneData> _bones = Array.Empty<BoneData>();
    private int[] _boneIndices = Array.Empty<int>();
    private Vector3[] _rotations = Array.Empty<Vector3>();
    private bool _updatingUI;

    public PoseEditorView()
    {
        InitializeComponent();
    }

    public event Action<int, Vector3>? RotationChanged;

    public Func<int, Vector3>? GetBoneRotation { get; set; }

    public void SetBones(IReadOnlyList<BoneData> bones)
    {
        _bones = bones;

        var names = new List<string>(bones.Count);
        var indices = new List<int>(bones.Count);
        var rotations = new List<Vector3>(bones.Count);

        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            if (bone.Ik != null || bone.Name.Contains("IK", StringComparison.OrdinalIgnoreCase))
                continue;

            names.Add(bone.Name);
            indices.Add(i);
            rotations.Add(GetBoneRotation?.Invoke(i) ?? Vector3.Zero);
        }

        _boneIndices = indices.ToArray();
        _rotations = rotations.ToArray();
        BonePicker.ItemsSource = names;
        if (names.Count > 0)
            BonePicker.SelectedIndex = 0;
    }

    private void OnBonePickerSelectedIndexChanged(object? sender, EventArgs e)
    {
        int index = BonePicker.SelectedIndex;
        if (index < 0 || index >= _boneIndices.Length)
            return;
        int boneIndex = _boneIndices[index];
        if (boneIndex < 0 || boneIndex >= _bones.Count)
            return;
        var rot = GetBoneRotation?.Invoke(boneIndex) ?? _rotations[index];
        _rotations[index] = rot;
        _updatingUI = true;
        XSlider.Value = rot.X;
        YSlider.Value = rot.Y;
        ZSlider.Value = rot.Z;
        _updatingUI = false;
    }

    private void OnSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (sender == XSlider)
        {
            XValue.Text = e.NewValue.ToString("F0");
        }
        else if (sender == YSlider)
        {
            YValue.Text = e.NewValue.ToString("F0");
        }
        else if (sender == ZSlider)
        {
            ZValue.Text = e.NewValue.ToString("F0");
        }

        if (_rotations.Length != _boneIndices.Length)
        {
            System.Diagnostics.Trace.WriteLine($"Rotation and bone index length mismatch: rotations={_rotations.Length}, boneIndices={_boneIndices.Length}");
            return;
        }

        int pickerIndex = BonePicker.SelectedIndex;
        if (pickerIndex >= 0 && pickerIndex < _boneIndices.Length)
        {
            var rot = new Vector3((float)XSlider.Value, (float)YSlider.Value, (float)ZSlider.Value);
            _rotations[pickerIndex] = rot;
            if (!_updatingUI)
            {
                int boneIndex = _boneIndices[pickerIndex];
                if (boneIndex >= 0 && boneIndex < _bones.Count)
                {
                    System.Diagnostics.Trace.WriteLine($"RotationChanged fired: index={boneIndex} rot={rot}");
                    RotationChanged?.Invoke(boneIndex, rot);
                }
            }
        }
    }
}
