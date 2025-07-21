using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.Import;
using OpenTK.Mathematics;

namespace MiniMikuDanceMaui;

public partial class IKView : ContentView
{
public event Action<string, string, double>? BoneValueChanged;
public event Action<string>? IkSolveRequested;

    private readonly Dictionary<string, Dictionary<string, BoneAxisControl>> _controls = new();

    public IKView()
    {
        InitializeComponent();
        BonePicker.ItemsSource = HumanoidBones.StandardOrder.ToList();
        BonePicker.SelectedIndexChanged += OnBoneSelected;
        var rangeValues = Enumerable.Range(0, 37).Select(i => (object)(i * 5)).ToList();
        var centerValues = Enumerable.Range(0, 37).Select(i => (object)(-180 + i * 10)).ToList();
        foreach (var name in HumanoidBones.StandardOrder)
        {
            var x = new BoneAxisControl { ValueFormat = "F0" };
            var y = new BoneAxisControl { ValueFormat = "F0" };
            var z = new BoneAxisControl { ValueFormat = "F0" };
            x.SetLabels("Rot", "X");
            y.SetLabels("Rot", "Y");
            z.SetLabels("Rot", "Z");
            foreach (var c in new[]{x,y,z})
            {
                c.SetPickers(centerValues, rangeValues, 0, 180);
                c.SetRange(-180, 180);
            }
            string captured = name;
            x.ValueChanged += v => BoneValueChanged?.Invoke(captured, "X", v);
            y.ValueChanged += v => BoneValueChanged?.Invoke(captured, "Y", v);
            z.ValueChanged += v => BoneValueChanged?.Invoke(captured, "Z", v);
            _controls[name] = new Dictionary<string, BoneAxisControl>
            {
                { "X", x },
                { "Y", y },
                { "Z", z }
            };
        }
        if (HumanoidBones.StandardOrder.Length > 0)
            BonePicker.SelectedIndex = 0;
        ShowSelectedBone();
    }

    private void OnBoneSelected(object? sender, EventArgs e) => ShowSelectedBone();

    private void ShowSelectedBone()
    {
        BoneList.Children.Clear();
        if (BonePicker.SelectedItem is string name && _controls.TryGetValue(name, out var dict))
        {
            BoneList.Children.Add(dict["X"]);
            BoneList.Children.Add(dict["Y"]);
            BoneList.Children.Add(dict["Z"]);
        }
    }

    public void Refresh()
    {
        ShowSelectedBone();
    }

    public Vector3 GetBoneValue(string bone)
    {
        if (_controls.TryGetValue(bone, out var d))
        {
            return new Vector3((float)d["X"].Value, (float)d["Y"].Value, (float)d["Z"].Value);
        }
        return Vector3.Zero;
    }
    public void SetBoneValue(string bone, Vector3 value)
    {
        if (_controls.TryGetValue(bone, out var d))
        {
            d["X"].Value = value.X;
            d["Y"].Value = value.Y;
            d["Z"].Value = value.Z;
        }
    }

    private void OnLeftIkClicked(object? sender, EventArgs e)
        => IkSolveRequested?.Invoke("left");

    private void OnRightIkClicked(object? sender, EventArgs e)
        => IkSolveRequested?.Invoke("right");
}
