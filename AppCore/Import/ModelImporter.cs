using Assimp;
using GLTFImage = SharpGLTF.Schema2.Image;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Linq;
using System.Diagnostics;
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

    public ModelData ImportModel(Stream stream)
    {
        Debug.WriteLine("[ModelImporter] Loading model from stream");
        var model = SharpGLTF.Schema2.ModelRoot.ReadGLB(stream);
        return ImportVrm(model);
    }

    public ModelData ImportModel(string path)
    {
        Debug.WriteLine($"[ModelImporter] Loading model: {path}");

        if (!File.Exists(path))
        {
            Debug.WriteLine($"[ModelImporter] File not found: {path}");
            throw new FileNotFoundException("Model file not found", path);
        }

        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".vrm" || ext == ".gltf" || ext == ".glb")
        {
            return ImportVrm(path);
        }

        var scene = _context.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals);
        Debug.WriteLine($"[ModelImporter] Imported generic model: {scene.Meshes[0].VertexCount} vertices");
        return new ModelData { Mesh = scene.Meshes[0] };
    }

    private ModelData ImportVrm(string path)
    {
        Debug.WriteLine($"[ModelImporter] Importing VRM: {path}");
        var model = SharpGLTF.Schema2.ModelRoot.Load(path);
        return ImportVrm(model);
    }

    private ModelData ImportVrm(SharpGLTF.Schema2.ModelRoot model)
    {
        var mesh = new Assimp.Mesh("mesh", Assimp.PrimitiveType.Triangle);

        var prim = model.LogicalMeshes.First().Primitives.First();
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

        GLTFImage? image = prim.Material?.FindChannel("BaseColor")?.Texture?.PrimaryImage
            ?? model.LogicalImages.FirstOrDefault();
        if (image != null)
        {
            using var stream = image.OpenImageFile();
            using var img = Image.Load<Rgba32>(stream);
            texW = img.Width;
            texH = img.Height;
            texBytes = new byte[texW * texH * 4];
            img.CopyPixelDataTo(texBytes);
        }

        var transform = System.Numerics.Matrix4x4.Identity;
        var node = model.DefaultScene?.VisualChildren.FirstOrDefault();
        if (node != null)
        {
            var m = node.WorldMatrix;
            transform = m;
        }

        Debug.WriteLine($"[ModelImporter] VRM loaded: {mesh.VertexCount} vertices");

        return new ModelData
        {
            Mesh = mesh,
            TextureData = texBytes,
            TextureWidth = texW,
            TextureHeight = texH,
            Transform = transform
        };
    }
}
