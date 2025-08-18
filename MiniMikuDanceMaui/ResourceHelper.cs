namespace MiniMikuDanceMaui;

using Microsoft.Maui.Controls;
using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiColors = Microsoft.Maui.Graphics.Colors;

public static class ResourceHelper
{
    public static MauiColor GetColor(string key)
    {
        if (Application.Current?.Resources?.TryGetValue(key, out var value) == true && value is MauiColor color)
        {
            return color;
        }
        return MauiColors.Black;
    }
}
