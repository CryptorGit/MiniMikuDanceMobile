using Microsoft.Maui.Controls;
using System.Collections.Generic;
using MiniMikuDance.Import;
using System.Numerics;

namespace MiniMikuDanceMaui;

public partial class BoneView : ContentView
{
    private readonly List<BoneItem> _bones = new();

    public BoneView()
    {
        InitializeComponent();
    }

    public void SetBones(IEnumerable<BoneData> bones)
    {
        _bones.Clear();
        foreach (var b in bones)
        {
            var e = ToEuler(b.Rotation);
            _bones.Add(new BoneItem { Name = b.Name, RotX = e.X, RotY = e.Y, RotZ = e.Z });
        }
        BuildGrid();
    }

    private void BuildGrid()
    {
        BoneGrid.RowDefinitions.Clear();
        BoneGrid.Children.Clear();
        BoneGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        AddCell(new Label { Text = "Bone", TextColor = Colors.White }, 0, 0);
        AddCell(new Label { Text = "X", TextColor = Colors.White }, 1, 0);
        AddCell(new Label { Text = "Y", TextColor = Colors.White }, 2, 0);
        AddCell(new Label { Text = "Z", TextColor = Colors.White }, 3, 0);
        int row = 1;
        foreach (var b in _bones)
        {
            BoneGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddCell(new Label { Text = b.Name, TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }, 0, row);
            var sx = CreateSlider(b, nameof(b.RotX));
            AddCell(sx, 1, row);
            var sy = CreateSlider(b, nameof(b.RotY));
            AddCell(sy, 2, row);
            var sz = CreateSlider(b, nameof(b.RotZ));
            AddCell(sz, 3, row);
            row++;
        }
    }

    private void AddCell(View view, int column, int row)
    {
        Grid.SetColumn(view, column);
        Grid.SetRow(view, row);
        BoneGrid.Children.Add(view);
    }
    private Slider CreateSlider(BoneItem item, string prop)
    {
        var s = new Slider { Minimum = -180, Maximum = 180, Value = GetValue(item, prop) };
        s.ValueChanged += (s, e) => SetValue(item, prop, e.NewValue);
        return s;
    }

    private static double GetValue(BoneItem item, string prop) => prop switch
    {
        nameof(BoneItem.RotX) => item.RotX,
        nameof(BoneItem.RotY) => item.RotY,
        nameof(BoneItem.RotZ) => item.RotZ,
        _ => 0
    };

    private static void SetValue(BoneItem item, string prop, double v)
    {
        switch (prop)
        {
            case nameof(BoneItem.RotX): item.RotX = v; break;
            case nameof(BoneItem.RotY): item.RotY = v; break;
            case nameof(BoneItem.RotZ): item.RotZ = v; break;
        }
    }

    private static Vector3 ToEuler(Quaternion q)
    {
        // simple conversion assuming yaw-pitch-roll order
        double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        double roll = Math.Atan2(sinr_cosp, cosr_cosp);

        double sinp = 2 * (q.W * q.Y - q.Z * q.X);
        double pitch;
        if (Math.Abs(sinp) >= 1)
            pitch = Math.CopySign(Math.PI / 2, sinp);
        else
            pitch = Math.Asin(sinp);

        double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        double yaw = Math.Atan2(siny_cosp, cosy_cosp);

        return new Vector3((float)(roll * 180 / Math.PI), (float)(pitch * 180 / Math.PI), (float)(yaw * 180 / Math.PI));
    }

    private class BoneItem
    {
        public string Name { get; set; } = string.Empty;
        public double RotX { get; set; }
        public double RotY { get; set; }
        public double RotZ { get; set; }
    }
}
