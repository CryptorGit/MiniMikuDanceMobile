using Microsoft.Maui.Controls;
using System.Collections.Generic;

namespace MiniMikuDanceMaui;

public partial class BoneView : ContentView
{
    // --- bone transformation values ---
    public double BoneRotationX { get; set; }
    public double BoneRotationY { get; set; }
    public double RotationZ { get; set; }
    public double BoneTranslationX { get; set; }
    public double BoneTranslationY { get; set; }
    public double TranslationZ { get; set; }

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

    public void SetRotationRange(double min, double max)
    {
        // Placeholder for future UI integration
    }

    public void SetTranslationRange(double min, double max)
    {
        // Placeholder for future UI integration
    }
}
