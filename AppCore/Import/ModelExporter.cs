using Assimp;

namespace MiniMikuDance.Import;

public class ModelExporter
{
    private readonly AssimpContext _context = new();

    public void ExportModel(ModelData model, string path)
    {
        if (model.Mesh == null)
            throw new ArgumentException("ModelData.Mesh is null");
        var scene = new Scene();
        scene.Meshes.Add(model.Mesh);
        scene.RootNode = new Node("root");
        scene.RootNode.MeshIndices.Add(0);
        var format = Path.GetExtension(path).TrimStart('.');
        _context.ExportFile(scene, path, format);
    }
}
