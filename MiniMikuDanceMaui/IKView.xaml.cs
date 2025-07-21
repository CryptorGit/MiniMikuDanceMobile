using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class IKView : ContentView
{
    public event Action<string, double>? BoneValueChanged;

    private readonly Dictionary<string, BoneAxisControl> _controls = new();

    public IKView()
    {
        InitializeComponent();
        var rangeValues = Enumerable.Range(0, 37).Select(i => (object)(i * 5)).ToList();
        var centerValues = Enumerable.Range(0, 37).Select(i => (object)(-180 + i * 10)).ToList();
        foreach (var name in HumanoidBones.StandardOrder)
        {
            var ctrl = new BoneAxisControl { ValueFormat = "F0" };
            ctrl.SetLabels("Rot", name);
            ctrl.SetPickers(centerValues, rangeValues, 0, 180);
            ctrl.SetRange(-180, 180);
            string captured = name;
            ctrl.ValueChanged += v => BoneValueChanged?.Invoke(captured, v);
            BoneList.Children.Add(ctrl);
            _controls[name] = ctrl;
        }
    }

    public double GetBoneValue(string bone)
        => _controls.TryGetValue(bone, out var c) ? c.Value : 0;

    public void SetBoneValue(string bone, double value)
    {
        if (_controls.TryGetValue(bone, out var c))
            c.Value = value;
    }
}
