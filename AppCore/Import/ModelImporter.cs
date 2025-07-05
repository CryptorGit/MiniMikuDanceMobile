using Assimp;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Numerics;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vector3D = Assimp.Vector3D;

namespace MiniMikuDance.Import;

public class ModelData
{
    public List<SubMeshData> SubMeshes { get; } = new();
    public Assimp.Mesh Mesh { get; set; } = null!;
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
}

public class ModelImporter
{
    private readonly AssimpContext _context = new();

    public ModelData ImportModel(Stream stream)
    {
        Debug.WriteLine("[ModelImporter] Loading model from stream");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        ms.Position = 0;
        var textureMap = ReadMainTextureIndices(bytes);
        var model = SharpGLTF.Schema2.ModelRoot.ReadGLB(ms);
        return ImportVrm(model, textureMap);
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
        var bytes = File.ReadAllBytes(path);
        using var ms = new MemoryStream(bytes);
        var textureMap = ReadMainTextureIndices(bytes);
        var model = SharpGLTF.Schema2.ModelRoot.ReadGLB(ms);
        return ImportVrm(model, textureMap);
    }

    private ModelData ImportVrm(SharpGLTF.Schema2.ModelRoot model, Dictionary<int, int> texMap)
    {
        var combined = new Assimp.Mesh("mesh", Assimp.PrimitiveType.Triangle);
        int combinedIndexOffset = 0;
        var data = new ModelData();

        foreach (var logical in model.LogicalMeshes)
        {
            foreach (var prim in logical.Primitives)
            {
                var sub = new Assimp.Mesh("mesh", Assimp.PrimitiveType.Triangle);
                var subUvs = new List<System.Numerics.Vector2>();
                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                var normals = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var uvs = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                var channel = prim.Material?.FindChannel("BaseColor");
                int matIndex = prim.Material?.LogicalIndex ?? -1;

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
                        var uv = uvs[i];
                        sub.TextureCoordinateChannels[0].Add(new Vector3D(uv.X, uv.Y, 0));
                        combined.TextureCoordinateChannels[0].Add(new Vector3D(uv.X, uv.Y, 0));
                        subUvs.Add(new System.Numerics.Vector2(uv.X, uv.Y));
                    }
                    else
                    {
                        sub.TextureCoordinateChannels[0].Add(new Vector3D(0, 0, 0));
                        combined.TextureCoordinateChannels[0].Add(new Vector3D(0, 0, 0));
                        subUvs.Add(System.Numerics.Vector2.Zero);
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

                var material = prim.Material;
                var colorParam = channel?.Parameter ?? Vector4.One;
                // VRM の alpha 値は利用せず常に不透明で描画
                colorParam.W = 1.0f;
                var colorFactor = colorParam;

                var smd = new SubMeshData
                {
                    Mesh = sub,
                    ColorFactor = colorFactor
                };
                smd.TexCoords.AddRange(subUvs);

                if (matIndex >= 0 && texMap.TryGetValue(matIndex, out var texIdx))
                {
                    var tex = model.LogicalTextures[texIdx];
                    var imgSeg = tex.PrimaryImage?.GetImageContent();
                    if (imgSeg.HasValue)
                    {
                        using var image = Image.Load<Rgba32>(imgSeg.Value.ToArray());
                        smd.TextureWidth = image.Width;
                        smd.TextureHeight = image.Height;
                        smd.TextureBytes = new byte[image.Width * image.Height * 4];
                        image.CopyPixelDataTo(smd.TextureBytes);
                    }
                }

                data.SubMeshes.Add(smd);
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

    private static Dictionary<int, int> ReadMainTextureIndices(byte[] glb)
    {
        var map = new Dictionary<int, int>();
        try
        {
            using var ms = new MemoryStream(glb);
            using var br = new BinaryReader(ms);
            uint magic = br.ReadUInt32();
            if (magic != 0x46546C67) return map; // 'glTF'
            br.ReadUInt32(); // version
            br.ReadUInt32(); // length
            uint jsonLen = br.ReadUInt32();
            uint chunkType = br.ReadUInt32();
            if (chunkType != 0x4E4F534A) return map; // JSON
            byte[] jsonBytes = br.ReadBytes((int)jsonLen);
            string json = System.Text.Encoding.UTF8.GetString(jsonBytes);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("extensions", out var exts) &&
                exts.TryGetProperty("VRM", out var vrmExt) &&
                vrmExt.TryGetProperty("materialProperties", out var matProps) &&
                matProps.ValueKind == System.Text.Json.JsonValueKind.Array &&
                root.TryGetProperty("materials", out var materials))
            {
                foreach (var m in matProps.EnumerateArray())
                {
                    if (!m.TryGetProperty("name", out var nameEl)) continue;
                    string? name = nameEl.GetString();
                    int matIndex = -1;
                    for (int i = 0; i < materials.GetArrayLength(); i++)
                    {
                        var mat = materials[i];
                        if (mat.TryGetProperty("name", out var nm) && nm.GetString() == name)
                        {
                            matIndex = i;
                            break;
                        }
                    }
                    if (matIndex >= 0 &&
                        m.TryGetProperty("textureProperties", out var texProps) &&
                        texProps.TryGetProperty("_MainTex", out var mainTexEl))
                    {
                        map[matIndex] = mainTexEl.GetInt32();
                    }
                }
            }

            if (root.TryGetProperty("materials", out var mats1))
            {
                for (int i = 0; i < mats1.GetArrayLength(); i++)
                {
                    var mat = mats1[i];
                    if (mat.TryGetProperty("extensions", out var mext) &&
                        mext.TryGetProperty("VRMC_materials_mtoon", out var mtoon) &&
                        mtoon.TryGetProperty("textures", out var texs) &&
                        texs.TryGetProperty("mainTexture", out var main) &&
                        main.TryGetProperty("index", out var idxEl))
                    {
                        map[i] = idxEl.GetInt32();
                    }
                }
            }

            if (root.TryGetProperty("materials", out var mats))
            {
                for (int i = 0; i < mats.GetArrayLength(); i++)
                {
                    if (map.ContainsKey(i)) continue;
                    var mat = mats[i];
                    if (mat.TryGetProperty("pbrMetallicRoughness", out var pbr) &&
                        pbr.TryGetProperty("baseColorTexture", out var tex) &&
                        tex.TryGetProperty("index", out var idxEl))
                    {
                        map[i] = idxEl.GetInt32();
                    }
                }
            }
        }
        catch
        {
        }
        return map;
    }
}
