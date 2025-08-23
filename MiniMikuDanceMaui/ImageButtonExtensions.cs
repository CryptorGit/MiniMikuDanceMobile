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
        var baseIcon = MauiIcon.GetValue(button) ?? new BaseIcon();
        baseIcon.IconColor = color;
        MauiIcon.SetValue(button, baseIcon);
    }
}
