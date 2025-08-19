using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class PhysicsView : ContentView
{
    public event Action<int, float>? RigidBodyMassChanged;

    private readonly List<RigidBodyData> _rigidBodies = new();
    private readonly List<JointData> _joints = new();

    public PhysicsView()
    {
        InitializeComponent();
    }

    public void SetPhysics(IEnumerable<RigidBodyData> rigidBodies, IEnumerable<JointData> joints)
    {
        _rigidBodies.Clear();
        _rigidBodies.AddRange(rigidBodies);
        _joints.Clear();
        _joints.AddRange(joints);

        PhysicsList.Children.Clear();
        var textColor = (Color)(Application.Current?.Resources?.TryGetValue("TextColor", out var color) == true ? color : Colors.Black);

        var rbHeader = new Label { Text = "RigidBodies", TextColor = textColor, FontAttributes = FontAttributes.Bold };
        PhysicsList.Children.Add(rbHeader);
        for (int i = 0; i < _rigidBodies.Count; i++)
        {
            var rb = _rigidBodies[i];
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 60 }
                }
            };
            var nameLabel = new Label { Text = rb.Name, TextColor = textColor };
            grid.Add(nameLabel);
            Grid.SetColumn(nameLabel, 0);
            var valueLabel = new Label { Text = rb.Mass.ToString("F2"), TextColor = textColor, HorizontalTextAlignment = TextAlignment.End };
            grid.Add(valueLabel);
            Grid.SetColumn(valueLabel, 1);
            PhysicsList.Children.Add(grid);
            var slider = new Slider { Minimum = 0, Maximum = 100, Value = rb.Mass };
            int index = i;
            slider.ValueChanged += (s, e) =>
            {
                valueLabel.Text = $"{e.NewValue:F2}";
                RigidBodyMassChanged?.Invoke(index, (float)e.NewValue);
            };
            PhysicsList.Children.Add(slider);
        }

        var jointHeader = new Label { Text = "Joints", TextColor = textColor, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0,10,0,0) };
        PhysicsList.Children.Add(jointHeader);
        foreach (var joint in _joints)
        {
            var label = new Label { Text = joint.Name, TextColor = textColor };
            PhysicsList.Children.Add(label);
        }
    }
}
