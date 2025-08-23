using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MauiIcons.Core;
using MauiIcons.Core.Base;
using MauiIcons.Material;

namespace MiniMikuDanceMaui;

public static class ImageButtonExtensions
{
    public static void SetIcon(this ImageButton button, MaterialIcons icon)
    {
        var color = (Color)button.GetValue(MauiIcon.IconColorProperty);
        MauiIcon.SetValue(button, new BaseIcon
        {
            Icon = icon,
            IconColor = color
        });
    }

    public static void SetIconColor(this ImageButton button, Color color)
    {
        MauiIcon.SetIconColor(button, color);
        var icon = MauiIcon.GetValue(button) as MaterialIcons;
        if (icon is not null)
        {
            button.Source = icon.ToImageSource(color);
        }
    }
}
