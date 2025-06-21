using Assimp;

namespace MiniMikuDance.Import;

public class ModelData
{
    public Mesh Mesh { get; set; } = null!;
}

public class ModelImporter
{
    private readonly AssimpContext _context = new();

    public ModelData ImportModel(string path)
    {
        var scene = _context.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals);
        return new ModelData { Mesh = scene.Meshes[0] };
    }
}
