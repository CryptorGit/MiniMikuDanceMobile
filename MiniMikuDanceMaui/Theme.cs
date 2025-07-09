using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace MiniMikuDanceMaui;

public static class Theme
{
    private static Color GetColor(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true)
        {
            if (value is Color c) return c;
            if (value is string s) return Color.FromArgb(s);
        }
        return Colors.Transparent;
    }

    private static SKColor ToSkColor(Color color)
    {
        return new SKColor(
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255),
            (byte)(color.Alpha * 255));
    }

    public static SKColor GetSkColor(string key) => ToSkColor(GetColor(key));

}
