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
        byte[]? texBytes = null;
        int texW = 0;
        int texH = 0;
        int indexOffset = 0;

        foreach (var logical in model.LogicalMeshes)
        {
            foreach (var prim in logical.Primitives)
            {
                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                var normals = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var uvs = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();

                for (int i = 0; i < positions.Count; i++)
                {
                    var v = positions[i];
                    // Z 軸を反転
                    mesh.Vertices.Add(new Vector3D(v.X, v.Y, -v.Z));
                    if (normals != null && i < normals.Count)
                    {
                        var n = normals[i];
                        mesh.Normals.Add(new Vector3D(n.X, n.Y, -n.Z));
                    }
                    else
                    {
                        mesh.Normals.Add(new Vector3D(0, 0, 1));
                    }
                    if (uvs != null && i < uvs.Count)
                    {
                        var uv = uvs[i];
                        mesh.TextureCoordinateChannels[0].Add(new Vector3D(uv.X, 1.0f - uv.Y, 0));
                    }
                    else
                    {
                        mesh.TextureCoordinateChannels[0].Add(new Vector3D(0, 0, 0));
                    }
                }

                var indices = prim.IndexAccessor.AsIndicesArray();
                for (int i = 0; i < indices.Count; i += 3)
                {
                    var face = new Face();
                    // Z 軸反転に合わせてインデックス順序を逆転
                    face.Indices.Add((int)indices[i] + indexOffset);
                    face.Indices.Add((int)indices[i + 2] + indexOffset);
                    face.Indices.Add((int)indices[i + 1] + indexOffset);
                    mesh.Faces.Add(face);
                }

                indexOffset += positions.Count;

                if (texBytes == null)
                {
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
                }
            }
        }

        var transform = System.Numerics.Matrix4x4.CreateScale(1f, 1f, -1f);
        var node = model.DefaultScene?.VisualChildren.FirstOrDefault();
        if (node != null)
        {
            var m = node.WorldMatrix;
            transform = System.Numerics.Matrix4x4.Multiply(transform, m);
        }

        VerifyMeshWinding(mesh);

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

    private static void VerifyMeshWinding(Assimp.Mesh mesh)
    {
        if (mesh.FaceCount == 0) return;
        var face = mesh.Faces[0];
        if (face.IndexCount < 3) return;
        var a = ToVector3(mesh.Vertices[face.Indices[0]]);
        var b = ToVector3(mesh.Vertices[face.Indices[1]]);
        var c = ToVector3(mesh.Vertices[face.Indices[2]]);
        var normal = face.Indices[0] < mesh.Normals.Count
            ? ToVector3(mesh.Normals[face.Indices[0]])
            : System.Numerics.Vector3.UnitZ;
        var cross = System.Numerics.Vector3.Cross(b - a, c - a);
        float dot = System.Numerics.Vector3.Dot(cross, normal);
        Debug.WriteLine($"[ModelImporter] First face winding {(dot >= 0 ? "CCW" : "CW")}");
    }

    private static System.Numerics.Vector3 ToVector3(Vector3D v)
        => new System.Numerics.Vector3(v.X, v.Y, v.Z);
}
