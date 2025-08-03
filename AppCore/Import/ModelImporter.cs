using Assimp;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vector3D = Assimp.Vector3D;
using MMDTools;

namespace MiniMikuDance.Import;

public class ModelData
{
    public List<SubMeshData> SubMeshes { get; set; } = new();
    public Assimp.Mesh Mesh { get; set; } = null!;
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
    public List<BoneData> Bones { get; set; } = new();
    public Dictionary<string, int> HumanoidBones { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(string Name, int Index)> HumanoidBoneList { get; set; } = new();
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
}

public class ModelImporter
{
    private readonly AssimpContext _context = new();

    public ModelData ImportModel(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        ms.Position = 0;

        // ファイルヘッダを確認して PMX かどうか判断する
        if (bytes.Length >= 4 &&
            bytes[0] == 'P' && bytes[1] == 'M' && bytes[2] == 'X' && bytes[3] == ' ')
        {
            return ImportPmx(ms);
        }

        throw new NotSupportedException("PMX 以外の形式には対応していません。");
    }

    public ModelData ImportModel(string path)
    {


        if (!File.Exists(path))
        {

            throw new FileNotFoundException("Model file not found", path);
        }

        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".pmx")
        {
            using var fs = File.OpenRead(path);
            return ImportPmx(fs, Path.GetDirectoryName(path));
        }

        var scene = _context.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals);

        return new ModelData { Mesh = scene.Meshes[0] };
    }

    private ModelData ImportPmx(Stream stream, string? baseDir = null)
    {
        var pmx = PMXParser.Parse(stream);
        var verts = pmx.VertexList.ToArray();
        var faces = pmx.SurfaceList.ToArray();
        var mats = pmx.MaterialList.ToArray();
        var texList = pmx.TextureList.ToArray();

        var combined = new Assimp.Mesh("pmx", Assimp.PrimitiveType.Triangle);
        for (int i = 0; i < verts.Length; i++)
        {
            var v = verts[i];
            combined.Vertices.Add(new Vector3D(v.Position.X, v.Position.Y, v.Position.Z));
            combined.Normals.Add(new Vector3D(v.Normal.X, v.Normal.Y, v.Normal.Z));
            combined.TextureCoordinateChannels[0].Add(new Vector3D(v.UV.X, v.UV.Y, 0));
        }
        foreach (var f in faces)
        {
            var face = new Face();
            face.Indices.Add(f.V1);
            face.Indices.Add(f.V2);
            face.Indices.Add(f.V3);
            combined.Faces.Add(face);
        }

        var data = new ModelData { Mesh = combined };
        int faceOffset = 0;
        string dir = baseDir ?? string.Empty;
        foreach (var mat in mats)
        {
            var sub = new Assimp.Mesh("pmx", Assimp.PrimitiveType.Triangle);
            var smd = new SubMeshData
            {
                Mesh = sub,
                ColorFactor = new System.Numerics.Vector4(mat.Diffuse.R, mat.Diffuse.G, mat.Diffuse.B, mat.Diffuse.A)
            };

            int faceCount = mat.VertexCount / 3;
            for (int i = 0; i < faceCount; i++)
            {
                var sf = faces[faceOffset + i];
                int[] idxs = { sf.V1, sf.V2, sf.V3 };
                int baseIndex = sub.Vertices.Count;
                for (int j = 0; j < 3; j++)
                {
                    var vv = verts[idxs[j]];
                    sub.Vertices.Add(new Vector3D(vv.Position.X, vv.Position.Y, vv.Position.Z));
                    sub.Normals.Add(new Vector3D(vv.Normal.X, vv.Normal.Y, vv.Normal.Z));
                    sub.TextureCoordinateChannels[0].Add(new Vector3D(vv.UV.X, vv.UV.Y, 0));
                    smd.TexCoords.Add(new System.Numerics.Vector2(vv.UV.X, vv.UV.Y));
                }
                var face = new Face();
                face.Indices.Add(baseIndex);
                face.Indices.Add(baseIndex + 1);
                face.Indices.Add(baseIndex + 2);
                sub.Faces.Add(face);
            }

            if (!string.IsNullOrEmpty(dir) && mat.Texture >= 0 && mat.Texture < texList.Length)
            {
                var texName = texList[mat.Texture];
                var texPath = Path.Combine(dir, texName);
                if (File.Exists(texPath))
                {
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(texPath);
                    smd.TextureWidth = image.Width;
                    smd.TextureHeight = image.Height;
                    smd.TextureBytes = new byte[image.Width * image.Height * 4];
                    image.CopyPixelDataTo(smd.TextureBytes);
                }
            }

            data.SubMeshes.Add(smd);
            faceOffset += faceCount;
        }

        return data;
    }
    // VRM 関連の機能は削除されました
}
