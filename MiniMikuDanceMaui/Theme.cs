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

    public static Color TimelineGridVerticalLineColor => GetColor(nameof(TimelineGridVerticalLineColor));
    public static Color TimelineGridHorizontalLineColor => GetColor(nameof(TimelineGridHorizontalLineColor));
    public static Color TimelineGridMajorLineColor => GetColor(nameof(TimelineGridMajorLineColor));
    public static Color TimelineGridLineColor => GetColor(nameof(TimelineGridLineColor));
    public static Color TimelineGridAccentColor => GetColor(nameof(TimelineGridAccentColor));
    public static SKColor TimelineGridVerticalLineSKColor => GetSkColor(nameof(TimelineGridVerticalLineColor));
    public static SKColor TimelineGridHorizontalLineSKColor => GetSkColor(nameof(TimelineGridHorizontalLineColor));
    public static SKColor TimelineGridMajorLineSKColor => GetSkColor(nameof(TimelineGridMajorLineColor));
    public static SKColor TimelineGridLineSKColor => GetSkColor(nameof(TimelineGridLineColor));
    public static SKColor TimelineGridAccentSKColor => GetSkColor(nameof(TimelineGridAccentColor));

    public static Color TimelineEvenRowColor => GetColor(nameof(TimelineEvenRowColor));
    public static Color TimelineOddRowColor => GetColor(nameof(TimelineOddRowColor));
    public static SKColor TimelineEvenRowSKColor => GetSkColor(nameof(TimelineEvenRowColor));
    public static SKColor TimelineOddRowSKColor => GetSkColor(nameof(TimelineOddRowColor));

    public static Color TimelineSelectionColor => GetColor(nameof(TimelineSelectionColor));
}
