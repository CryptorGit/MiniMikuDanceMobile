using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MauiIcons.Core;
using MauiIcons.Material;

namespace MiniMikuDanceMaui;

public static class ImageButtonExtensions
{
    public static void SetIcon(this ImageButton button, MaterialIcons icon)
    {
        MauiIcon.SetIcon(button, icon);
        var color = (Color)button.GetValue(MauiIcon.IconColorProperty);
        button.Source = icon.ToImageSource(color);
    }

    public static void SetIconColor(this ImageButton button, Color color)
    {
        MauiIcon.SetIconColor(button, color, false);
        if (MauiIcon.GetValue(button) is MaterialIcons icon)
        {
            button.Source = icon.ToImageSource(color);
        }
    }
}
