using Microsoft.Maui.Controls;
using System.Collections.Generic;
using MiniMikuDance.Import;
using MauiIcons.Core;
using MauiIcons.Material;

namespace MiniMikuDanceMaui;

public partial class BoneView : ContentView
{
    public BoneView()
    {
        InitializeComponent();
    }

    public void SetBones(IEnumerable<BoneData> bones)
    {
        BoneList.Children.Clear();
        var textColor = (Color)(Application.Current?.Resources?.TryGetValue("TextColor", out var color) == true ? color : Colors.Black);
        foreach (var bone in bones)
        {
            BoneList.Children.Add(new HorizontalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new MauiIcon { Icon = MaterialIcons.AccessibilityNew, IconColor = textColor },
                    new Label { Text = bone.Name, TextColor = textColor }
                }
            });
        }
    }
}
