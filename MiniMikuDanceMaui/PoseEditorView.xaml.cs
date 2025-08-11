using Microsoft.Maui.Controls;
using MiniMikuDance.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MiniMikuDanceMaui;

public partial class PoseEditorView : ContentView
{
    private IReadOnlyList<BoneData> _bones = Array.Empty<BoneData>();
    private readonly List<int> _boneIndices = new();

    public PoseEditorView()
    {
        InitializeComponent();
    }

    public event Action<int, Vector3>? RotationChanged;

    public Func<int, Vector3>? GetRotation;

    public void SetBones(IReadOnlyList<BoneData> bones)
    {
        _bones = bones;
        _boneIndices.Clear();
        for (int i = 0; i < _bones.Count; i++)
        {
            _boneIndices.Add(i);
        }
        BonePicker.ItemsSource = _boneIndices.Select(i => _bones[i].Name).ToList();
        BonePicker.SelectedIndex = _boneIndices.Count > 0 ? 0 : -1;
        UpdateSliders();
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

        int sel = BonePicker.SelectedIndex;
        if (sel >= 0 && sel < _boneIndices.Count)
        {
            int index = _boneIndices[sel];
            var rot = new Vector3((float)XSlider.Value, (float)YSlider.Value, (float)ZSlider.Value);
            RotationChanged?.Invoke(index, rot);
        }
    }

    private void OnBonePickerSelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateSliders();
    }

    private void UpdateSliders()
    {
        int sel = BonePicker.SelectedIndex;
        if (sel >= 0 && sel < _boneIndices.Count)
        {
            int index = _boneIndices[sel];
            var rot = GetRotation?.Invoke(index) ?? Vector3.Zero;
            XSlider.Value = rot.X;
            YSlider.Value = rot.Y;
            ZSlider.Value = rot.Z;
            XValue.Text = rot.X.ToString("F0");
            YValue.Text = rot.Y.ToString("F0");
            ZValue.Text = rot.Z.ToString("F0");
        }
    }
}
