using System.Linq;
using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

public partial class ModelInfoView : ContentView
{
    public ModelInfoView()
    {
        InitializeComponent();
    }

    public void SetModel(MiniMikuDance.Import.ModelData? model)
    {
        if (model == null)
        {
            IsVisible = false;
            return;
        }

        var vertexCount = model.SubMeshes.Sum(s => s.Mesh.Vertices.Count);
        VertexCount.Text = vertexCount.ToString();
        BoneCount.Text = model.Bones.Count.ToString();
        MorphCount.Text = model.Morphs.Count.ToString();
        IsVisible = true;
    }
}
