using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MiniMikuDanceMaui.Helpers;

public static class ResourceHelper
{
    public static Color GetColor(string key)
    {
        if (Application.Current?.Resources?.TryGetValue(key, out var value) == true && value is Color color)
        {
            return color;
        }
        return Colors.Black;
    }
}
