using Assimp;
using MiniMikuDance.Data;

namespace MiniMikuDance.Import;

public class ModelExporter
{
    private readonly AssimpContext _context = new();

    public void ExportModel(MmdModel model, string path)
    {
        if (model.Mesh == null)
            throw new ArgumentException("model.Mesh is null");
        var scene = new Scene();
        scene.Meshes.Add(model.Mesh);
        scene.RootNode = new Node("root");
        scene.RootNode.MeshIndices.Add(0);
        var format = Path.GetExtension(path).TrimStart('.');
        _context.ExportFile(scene, path, format);
    }
}
