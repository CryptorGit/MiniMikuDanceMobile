using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public static class ResourceHelper
{
    public static Color GetColor(string key, Color fallback)
    {
        return Application.Current?.Resources?.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;
    }
}
