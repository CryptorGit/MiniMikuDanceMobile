using Assimp;
using GLTFImage = SharpGLTF.Schema2.Image;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Numerics;
using System.Collections.Generic;
using Vector3D = Assimp.Vector3D;

namespace MiniMikuDance.Import;

public class ModelData
{
    public List<SubMeshData> SubMeshes { get; } = new();
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
        var combined = new Assimp.Mesh("mesh", Assimp.PrimitiveType.Triangle);
        int combinedIndexOffset = 0;
        var data = new ModelData();

        foreach (var logical in model.LogicalMeshes)
        {
            foreach (var prim in logical.Primitives)
            {
                var sub = new Assimp.Mesh("mesh", Assimp.PrimitiveType.Triangle);
                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                var normals = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var channel = prim.Material?.FindChannel("BaseColor");
                var texInfo = channel?.Texture;
                int texCoordIndex = channel?.TextureTransform?.TextureCoordinateOverride ?? channel?.TextureCoordinate ?? 0;
                var uvs = prim.GetVertexAccessor($"TEXCOORD_{texCoordIndex}")?.AsVector2Array();

                for (int i = 0; i < positions.Count; i++)
                {
                    var v = positions[i];
                    // Z 軸を反転
                    var pos = new Vector3D(v.X, v.Y, -v.Z);
                    sub.Vertices.Add(pos);
                    combined.Vertices.Add(pos);

                    if (normals != null && i < normals.Count)
                    {
                        var n = new Vector3D(normals[i].X, normals[i].Y, -normals[i].Z);
                        sub.Normals.Add(n);
                        combined.Normals.Add(n);
                    }
                    else
                    {
                        sub.Normals.Add(new Vector3D(0, 0, 1));
                        combined.Normals.Add(new Vector3D(0, 0, 1));
                    }
                    if (uvs != null && i < uvs.Count)
                    {
                        var tt = channel?.TextureTransform;
                        var mat = System.Numerics.Matrix3x2.Identity;
                        if (tt != null)
                        {
                            mat = System.Numerics.Matrix3x2.CreateScale(tt.Scale)
                                * System.Numerics.Matrix3x2.CreateRotation(tt.Rotation)
                                * System.Numerics.Matrix3x2.CreateTranslation(tt.Offset);
                        }
                        var uv = System.Numerics.Vector2.Transform(uvs[i], mat);
                        var texCoord = new Vector3D(uv.X, uv.Y, 0);
                        sub.TextureCoordinateChannels[0].Add(texCoord);
                        combined.TextureCoordinateChannels[0].Add(texCoord);
                    }
                    else
                    {
                        sub.TextureCoordinateChannels[0].Add(new Vector3D(0, 0, 0));
                        combined.TextureCoordinateChannels[0].Add(new Vector3D(0, 0, 0));
                    }
                }

                var indices = prim.IndexAccessor.AsIndicesArray();
                for (int i = 0; i < indices.Count; i += 3)
                {
                    // Z 軸反転に合わせてインデックス順序を逆転
                    var face = new Face();
                    face.Indices.Add((int)indices[i]);
                    face.Indices.Add((int)indices[i + 2]);
                    face.Indices.Add((int)indices[i + 1]);
                    sub.Faces.Add(face);

                    var cFace = new Face();
                    cFace.Indices.Add((int)indices[i] + combinedIndexOffset);
                    cFace.Indices.Add((int)indices[i + 2] + combinedIndexOffset);
                    cFace.Indices.Add((int)indices[i + 1] + combinedIndexOffset);
                    combined.Faces.Add(cFace);
                }

                combinedIndexOffset += positions.Count;

                byte[]? texBytes = null;
                int texW = 0;
                int texH = 0;
                var material = prim.Material;
                // channel is already defined above; reuse it
                GLTFImage? image = channel?.Texture?.PrimaryImage;
                var colorParam = channel?.Parameter ?? Vector4.One;
                var colorFactor = colorParam;
                if (image != null)
                {
                    using var stream = image.OpenImageFile();
                    using var img = Image.Load<Rgba32>(stream);
                    img.Mutate(x => x.Flip(FlipMode.Vertical));
                    texW = img.Width;
                    texH = img.Height;
                    texBytes = new byte[texW * texH * 4];
                    img.CopyPixelDataTo(texBytes);

                    if (data.TextureData == null)
                    {
                        data.TextureData = texBytes;
                        data.TextureWidth = texW;
                        data.TextureHeight = texH;
                    }
                }

                data.SubMeshes.Add(new SubMeshData
                {
                    Mesh = sub,
                    TextureData = texBytes,
                    TextureWidth = texW,
                    TextureHeight = texH,
                    ColorFactor = colorFactor
                });
            }
        }

        var transform = System.Numerics.Matrix4x4.Identity;
        var node = model.DefaultScene?.VisualChildren.FirstOrDefault();
        if (node != null)
        {
            transform = node.WorldMatrix;
        }

        VerifyMeshWinding(combined);

        Debug.WriteLine($"[ModelImporter] VRM loaded: {combined.VertexCount} vertices");

        data.Mesh = combined;
        data.Transform = transform;
        return data;
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
