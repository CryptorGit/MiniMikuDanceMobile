using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MauiIcons.Core;
using MauiIcons.Material;

namespace MiniMikuDanceMaui;

public static class ImageButtonExtensions
{
    public static void SetIcon(this ImageButton button, MaterialIcons icon)
    {
        MauiIcon.SetValue(button, icon);
        var color = MauiIcon.GetIconColor(button);
        button.ImageSource = icon.ToImageSource(color);
    }

    public static void SetIconColor(this ImageButton button, Color color)
    {
        MauiIcon.SetIconColor(button, color);
        var icon = (MaterialIcons)MauiIcon.GetValue(button);
        button.ImageSource = icon.ToImageSource(color);
    }
}
