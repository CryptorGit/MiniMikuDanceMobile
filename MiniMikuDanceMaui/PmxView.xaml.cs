using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public partial class PmxView : ContentView
{
    public PmxView()
    {
        InitializeComponent();
    }

    public void SetModel(MiniMikuDance.Import.ModelData? model, PmxRenderer renderer)
    {
        if (model == null)
        {
            WarningLabel.IsVisible = true;
            SubMeshList.IsVisible = false;
            return;
        }

        WarningLabel.IsVisible = false;
        SubMeshList.ItemsSource = renderer.SubMeshInfos;
        SubMeshList.IsVisible = true;
    }
}
