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
public event Action<string, string, double>? PositionValueChanged;
public event Action<string>? IkSolveRequested;

    private readonly Dictionary<string, Dictionary<string, BoneAxisControl>> _controls = new();
    private readonly Dictionary<string, Dictionary<string, BoneAxisControl>> _posControls = new();

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

        var posRangeValues = Enumerable.Range(0, 11).Select(i => (object)(i / 10f)).ToList();
        var posCenterValues = Enumerable.Range(0, 21).Select(i => (object)(-1f + i * 0.1f)).ToList();
        string[] endBones = { "leftHand", "rightHand", "leftFoot", "rightFoot" };
        foreach (var name in endBones)
        {
            var px = new BoneAxisControl { ValueFormat = "F2" };
            var py = new BoneAxisControl { ValueFormat = "F2" };
            var pz = new BoneAxisControl { ValueFormat = "F2" };
            px.SetLabels("Pos", "X");
            py.SetLabels("Pos", "Y");
            pz.SetLabels("Pos", "Z");
            foreach (var c in new[] { px, py, pz })
            {
                c.SetPickers(posCenterValues, posRangeValues, 0f, 1f);
                c.SetRange(-1, 1);
            }
            string captured = name;
            px.ValueChanged += v => PositionValueChanged?.Invoke(captured, "X", v);
            py.ValueChanged += v => PositionValueChanged?.Invoke(captured, "Y", v);
            pz.ValueChanged += v => PositionValueChanged?.Invoke(captured, "Z", v);
            _posControls[name] = new Dictionary<string, BoneAxisControl>
            {
                { "X", px },
                { "Y", py },
                { "Z", pz }
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
            if (_posControls.TryGetValue(name, out var pdict))
            {
                BoneList.Children.Add(pdict["X"]);
                BoneList.Children.Add(pdict["Y"]);
                BoneList.Children.Add(pdict["Z"]);
            }
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

    public Vector3 GetPositionValue(string bone)
    {
        if (_posControls.TryGetValue(bone, out var d))
        {
            return new Vector3((float)d["X"].Value, (float)d["Y"].Value, (float)d["Z"].Value);
        }
        return Vector3.Zero;
    }

    public void SetPositionValue(string bone, Vector3 value)
    {
        if (_posControls.TryGetValue(bone, out var d))
        {
            d["X"].Value = value.X;
            d["Y"].Value = value.Y;
            d["Z"].Value = value.Z;
        }
    }

    private void OnLeftLegIkClicked(object? sender, EventArgs e)
        => IkSolveRequested?.Invoke("leftLeg");

    private void OnRightLegIkClicked(object? sender, EventArgs e)
        => IkSolveRequested?.Invoke("rightLeg");

    private void OnLeftArmIkClicked(object? sender, EventArgs e)
        => IkSolveRequested?.Invoke("leftArm");

    private void OnRightArmIkClicked(object? sender, EventArgs e)
        => IkSolveRequested?.Invoke("rightArm");
}
