using Microsoft.Maui.Controls;
using MiniMikuDance.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MiniMikuDanceMaui;

public partial class PoseEditorView : ContentView
{
    public PoseEditorView()
    {
        InitializeComponent();
    }

    public event Action<int, Vector3>? RotationChanged;

    public void SetBones(IReadOnlyList<BoneData> bones)
    {
        BonePicker.ItemsSource = bones.Select(b => b.Name).ToList();
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

        int index = BonePicker.SelectedIndex;
        if (index >= 0)
        {
            var rot = new Vector3((float)XSlider.Value, (float)YSlider.Value, (float)ZSlider.Value);
            RotationChanged?.Invoke(index, rot);
        }
    }
}
