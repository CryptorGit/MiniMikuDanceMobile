using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public class RefreshableScrollView : ScrollView
{
    public void RefreshLayout()
    {
        InvalidateMeasure();
    }
}
