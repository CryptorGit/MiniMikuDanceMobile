using Assimp;
using SharpGLTF.Schema2;
using GLTFImage = SharpGLTF.Schema2.Image;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using MMDTools; // PMXParser
using System.IO;
using Vector3D = Assimp.Vector3D;

namespace MiniMikuDance.Import;

public class ModelData
{
    public Assimp.Mesh Mesh { get; set; } = null!;
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
    public byte[]? TextureData { get; set; }
    public int TextureWidth { get; set; }
    public int TextureHeight { get; set; }
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
        if (ext == ".pmx")
        {
            using var fs = File.OpenRead(path);
            return ImportPmx(fs);
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
        var uvs = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
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
        if (uvs != null)
        {
            for (int i = 0; i < uvs.Count; i++)
            {
                var uv = uvs[i];
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(uv.X, uv.Y, 0));
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
        byte[]? texBytes = null;
        int texW = 0;
        int texH = 0;
        GLTFImage? image = model.LogicalImages.FirstOrDefault();
        if (image != null)
        {
            // Texture loading not implemented in this environment
        }
        return new ModelData
        {
            Mesh = mesh,
            TextureData = texBytes,
            TextureWidth = texW,
            TextureHeight = texH
        };
    }

    private ModelData ImportPmx(Stream pmxStream)
    {
        PMXObject pmx = PMXParser.Parse(pmxStream);
        var verts = pmx.VertexList.Span;
        var faces = pmx.SurfaceList.Span;

        var mesh = new Assimp.Mesh("pmx", Assimp.PrimitiveType.Triangle);
        foreach (var v in verts)
        {
            mesh.Vertices.Add(new Vector3D((float)v.Position.X, (float)v.Position.Y, (float)v.Position.Z));
            mesh.Normals.Add(new Vector3D((float)v.Normal.X, (float)v.Normal.Y, (float)v.Normal.Z));
            mesh.TextureCoordinateChannels[0].Add(new Vector3D((float)v.UV.X, (float)v.UV.Y, 0f));
        }

        foreach (var f in faces)
        {
            var face = new Face();
            face.Indices.Add((int)f.V1);
            face.Indices.Add((int)f.V2);
            face.Indices.Add((int)f.V3);
            mesh.Faces.Add(face);
        }

        return new ModelData { Mesh = mesh };
    }
}
