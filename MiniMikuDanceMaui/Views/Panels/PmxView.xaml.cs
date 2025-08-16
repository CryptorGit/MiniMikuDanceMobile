using System.Linq;
using Microsoft.Maui.Controls;
using MiniMikuDance.Import;
using MiniMikuDanceMaui.Utilities;

namespace MiniMikuDanceMaui.Views.Panels;

public partial class PmxView : ContentView
{
    private ModelData? _model;

    public PmxView()
    {
        InitializeComponent();
    }

    public void SetModel(ModelData? model)
    {
        _model = model;
        UpdateView();
    }

    private void UpdateView()
    {
        if (_model == null)
        {
            WarningLabel.IsVisible = true;
            SubMeshList.IsVisible = false;
            return;
        }

        WarningLabel.IsVisible = false;
        var items = _model.SubMeshes.Select((s, i) => new SubMeshInfo
        {
            Index = i,
            Texture = s.TextureFilePath ?? "(none)",
            Size = $"{s.TextureWidth}x{s.TextureHeight}"
        }).ToList();
        SubMeshList.ItemsSource = items;
        SubMeshList.IsVisible = true;
    }
}
