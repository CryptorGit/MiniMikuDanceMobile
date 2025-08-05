using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public class RefreshableScrollView : ScrollView
{
    public void RefreshLayout()
    {
        InvalidateMeasure();
    }

    // 互換性のための別名メソッド
    public void UpdateLayout() => RefreshLayout();
}
