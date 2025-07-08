using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using System.Threading.Tasks;

namespace MiniMikuDanceMaui;

public static class MauiExtensions
{
    public static SKColor ToSKColor(this Color color)
    {
        return new SKColor(
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255),
            (byte)(color.Alpha * 255));
    }

    public static Task ScrollToAsync(this CollectionView view, int index, int groupIndex, ScrollToPosition position, bool animate)
    {
        view.ScrollTo(index, groupIndex, position, animate);
        return Task.CompletedTask;
    }
}
