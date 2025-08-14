using Assimp;

namespace MiniMikuDance.Import;

public class ModelExporter : IDisposable
{
    private AssimpContext? _context;

    public void ExportModel(ModelData model, string path)
    {
        if (model.Mesh == null)
            throw new ArgumentException("ModelData.Mesh is null");
        _context = new AssimpContext();
        try
        {
            var scene = new Scene();
            scene.Meshes.Add(model.Mesh);
            scene.RootNode = new Node("root");
            scene.RootNode.MeshIndices.Add(0);
            var format = Path.GetExtension(path).TrimStart('.');
            _context.ExportFile(scene, path, format);
        }
        finally
        {
            _context.Dispose();
            _context = null;
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
