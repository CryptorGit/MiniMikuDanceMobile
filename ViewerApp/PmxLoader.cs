using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Mathematics;
using MMDTools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ViewerApp;

internal class PmxSubMesh
{
    public float[] Positions = Array.Empty<float>();
    public float[] Normals = Array.Empty<float>();
    public float[] TexCoords = Array.Empty<float>();
    public uint[] Indices = Array.Empty<uint>();
    public Vector4 ColorFactor = Vector4.One;
    public byte[]? TextureBytes;
    public int TextureWidth;
    public int TextureHeight;
}

internal class PmxModel
{
    public List<PmxSubMesh> SubMeshes { get; } = new();
    public Matrix4 Transform = Matrix4.Identity;
}

internal static class PmxLoader
{
    public static PmxModel Load(string path)
    {
        using var fs = File.OpenRead(path);
        var pmx = PMXParser.Parse(fs);
        var verts = pmx.VertexList.ToArray();
        var faces = pmx.SurfaceList.ToArray();
        var mats = pmx.MaterialList.ToArray();
        var texList = pmx.TextureList.ToArray();
        var model = new PmxModel();
        int faceOffset = 0;
        string baseDir = Path.GetDirectoryName(path) ?? string.Empty;
        foreach (var mat in mats)
        {
            var pos = new List<float>();
            var norm = new List<float>();
            var uv = new List<float>();
            var idx = new List<uint>();
            int faceCount = mat.VertexCount / 3;
            for (int i = 0; i < faceCount; i++)
            {
                var f = faces[faceOffset + i];
                int[] ids = { f.V1, f.V2, f.V3 };
                for (int j = 0; j < 3; j++)
                {
                    var v = verts[ids[j]];
                    pos.Add(v.Position.X);
                    pos.Add(v.Position.Y);
                    pos.Add(v.Position.Z);
                    norm.Add(v.Normal.X);
                    norm.Add(v.Normal.Y);
                    norm.Add(v.Normal.Z);
                    uv.Add(v.UV.X);
                    uv.Add(v.UV.Y);
                    idx.Add((uint)idx.Count);
                }
            }
            var sm = new PmxSubMesh
            {
                Positions = pos.ToArray(),
                Normals = norm.ToArray(),
                TexCoords = uv.ToArray(),
                Indices = idx.ToArray(),
                ColorFactor = new Vector4(mat.Diffuse.R, mat.Diffuse.G, mat.Diffuse.B, mat.Diffuse.A)
            };
            if (mat.Texture >= 0 && mat.Texture < texList.Length)
            {
                var texName = texList[mat.Texture];
                var texPath = Path.Combine(baseDir, texName);
                if (File.Exists(texPath))
                {
                    using var image = Image.Load<Rgba32>(texPath);
                    sm.TextureWidth = image.Width;
                    sm.TextureHeight = image.Height;
                    sm.TextureBytes = new byte[image.Width * image.Height * 4];
                    image.CopyPixelDataTo(sm.TextureBytes);
                }
            }
            model.SubMeshes.Add(sm);
            faceOffset += faceCount;
        }
        return model;
    }
}
