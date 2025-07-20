using Microsoft.Maui.Controls;
using System.Collections.Generic;

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
        var textColor = (Color)Application.Current.Resources["TextColor"];
        foreach (var name in bones)
        {
            BoneList.Children.Add(new Label { Text = name, TextColor = textColor });
        }
    }
}
