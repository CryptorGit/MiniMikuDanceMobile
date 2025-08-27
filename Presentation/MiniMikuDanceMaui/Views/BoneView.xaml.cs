using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;

namespace MiniMikuDanceMaui;

public partial class BoneView : ContentView
{
    public event Action<int>? BoneTapped;

    private readonly List<Label> _labels = new();

    public BoneView()
    {
        InitializeComponent();
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BoneList.Children.Clear();
        _labels.Clear();
        var textColor = (Color)(Application.Current?.Resources?.TryGetValue("TextColor", out var color) == true ? color : Colors.Black);
        int idx = 0;
        foreach (var name in bones)
        {
            var label = new Label { Text = name, TextColor = textColor };
            int captured = idx;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => BoneTapped?.Invoke(captured);
            label.GestureRecognizers.Add(tap);
            BoneList.Children.Add(label);
            _labels.Add(label);
            idx++;
        }
    }

    public void HighlightBone(int index)
    {
        for (int i = 0; i < _labels.Count; i++)
        {
            _labels[i].BackgroundColor = i == index ? Colors.LightGray : Colors.Transparent;
        }
    }
}
