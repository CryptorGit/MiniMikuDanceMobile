using Assimp;
using SharpGLTF.Schema2;
using Vector3D = Assimp.Vector3D;

namespace MiniMikuDance.Import;

public class ModelData
{
    public Assimp.Mesh Mesh { get; set; } = null!;
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
}

public class ModelImporter
{
    private readonly AssimpContext _context = new();

    public ModelData ImportModel(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Model file not found", path);

        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".vrm" || ext == ".gltf" || ext == ".glb")
        {
            return ImportVrm(path);
        }

        var scene = _context.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals);
        return new ModelData { Mesh = scene.Meshes[0] };
    }

    private ModelData ImportVrm(string path)
    {
        var model = ModelRoot.Load(path);
        var mesh = new Assimp.Mesh("mesh", Assimp.PrimitiveType.Triangle);
        var prim = model.LogicalMeshes[0].Primitives[0];
        var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
        var normals = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
        foreach (var v in positions)
        {
            mesh.Vertices.Add(new Vector3D(v.X, v.Y, v.Z));
        }
        if (normals != null)
        {
            foreach (var n in normals)
            {
                mesh.Normals.Add(new Vector3D(n.X, n.Y, n.Z));
            }
        }
        var indices = prim.IndexAccessor.AsIndicesArray();
        for (int i = 0; i < indices.Count; i += 3)
        {
            var face = new Face();
            face.Indices.Add((int)indices[i]);
            face.Indices.Add((int)indices[i + 1]);
            face.Indices.Add((int)indices[i + 2]);
            mesh.Faces.Add(face);
        }
        return new ModelData { Mesh = mesh };
    }
}
