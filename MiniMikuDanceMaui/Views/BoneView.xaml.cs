using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;
using MiniMikuDanceMaui.Renderers.Pmx;
using MauiIcons.Core;
using MauiIcons.Material;

namespace MiniMikuDanceMaui.Views;

public partial class BoneView : ContentView
{
    public PmxRenderer? Renderer { get; set; }

    public BoneView()
    {
        InitializeComponent();
    }

    public void SetBones(IEnumerable<BoneData> bones)
    {
        BoneList.Children.Clear();
        var textColor = (Color)(Application.Current?.Resources?.TryGetValue("TextColor", out var color) == true ? color : Colors.Black);
        int index = 0;
        foreach (var bone in bones)
        {
            var layout = new HorizontalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new MauiIcon { Icon = MaterialIcons.AccessibilityNew, IconColor = textColor },
                    new Label { Text = bone.Name, TextColor = textColor }
                }
            };
            int current = index;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => Renderer?.SelectBone(current);
            layout.GestureRecognizers.Add(tap);
            BoneList.Children.Add(layout);
            index++;
        }

        var upButton = new Button { Text = "Up" };
        upButton.Clicked += (s, e) => Renderer?.TranslateSelectedBone(new Vector3(0f, 0.1f, 0f));
        var downButton = new Button { Text = "Down" };
        downButton.Clicked += (s, e) => Renderer?.TranslateSelectedBone(new Vector3(0f, -0.1f, 0f));
        BoneList.Children.Add(new HorizontalStackLayout
        {
            Spacing = 4,
            Children = { upButton, downButton }
        });
    }
}
