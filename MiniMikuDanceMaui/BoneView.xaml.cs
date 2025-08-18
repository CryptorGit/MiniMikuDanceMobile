using Microsoft.Maui.Controls;
using System.Collections.Generic;
using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiColors = Microsoft.Maui.Graphics.Colors;

namespace MiniMikuDanceMaui;

public partial class BoneView : ContentView
{
    public BoneView()
    {
        InitializeComponent();
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BoneList.Children.Clear();
        var textColor = (MauiColor)(Application.Current?.Resources?.TryGetValue("TextColor", out var color) == true ? color : MauiColors.Black);
        foreach (var name in bones)
        {
            BoneList.Children.Add(new Label { Text = name, TextColor = textColor });
        }
    }
}
