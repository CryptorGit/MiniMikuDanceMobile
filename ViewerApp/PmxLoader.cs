using System;
using System.Collections.Generic;
using System.IO;
using MMDTools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using OtkMatrix4 = OpenTK.Mathematics.Matrix4;
using OtkVector4 = OpenTK.Mathematics.Vector4;

namespace ViewerApp;

internal class PmxSubMesh
{
    public float[] Positions = Array.Empty<float>();
    public float[] Normals = Array.Empty<float>();
    public float[] TexCoords = Array.Empty<float>();
    public uint[] Indices = Array.Empty<uint>();
    public OtkVector4 ColorFactor = OtkVector4.One;
    public byte[]? TextureBytes;
    public int TextureWidth;
    public int TextureHeight;
}

internal class PmxModel
{
    public List<PmxSubMesh> SubMeshes { get; } = new();
    public OtkMatrix4 Transform = OtkMatrix4.Identity;
}

internal static class PmxLoader
{
    public const float DefaultScale = 0.1f;

    public static PmxModel Load(string path, float scale = DefaultScale)
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
        Span<int> ids = stackalloc int[3];
        foreach (var mat in mats)
        {
            int faceCount = mat.VertexCount / 3;
            int vertexCount = faceCount * 3;
            float[] pos = new float[vertexCount * 3];
            float[] norm = new float[vertexCount * 3];
            float[] uv = new float[vertexCount * 2];
            uint[] idx = new uint[vertexCount];
            var posSpan = pos.AsSpan();
            var normSpan = norm.AsSpan();
            var uvSpan = uv.AsSpan();
            var idxSpan = idx.AsSpan();
            int posIndex = 0;
            int normIndex = 0;
            int uvIndex = 0;
            int idxIndex = 0;
            for (int i = 0; i < faceCount; i++)
            {
                var f = faces[faceOffset + i];
                ids[0] = f.V1;
                ids[1] = f.V2;
                ids[2] = f.V3;
                for (int j = 0; j < 3; j++)
                {
                    var v = verts[ids[j]];
                    posSpan[posIndex++] = v.Position.X * scale;
                    posSpan[posIndex++] = v.Position.Y * scale;
                    posSpan[posIndex++] = v.Position.Z * scale;
                    normSpan[normIndex++] = v.Normal.X;
                    normSpan[normIndex++] = v.Normal.Y;
                    normSpan[normIndex++] = v.Normal.Z;
                    uvSpan[uvIndex++] = v.UV.X;
                    uvSpan[uvIndex++] = v.UV.Y;
                    idxSpan[idxIndex] = (uint)idxIndex;
                    idxIndex++;
                }
            }
            var sm = new PmxSubMesh
            {
                Positions = pos,
                Normals = norm,
                TexCoords = uv,
                Indices = idx,
                ColorFactor = new OtkVector4(mat.Diffuse.R, mat.Diffuse.G, mat.Diffuse.B, mat.Diffuse.A)
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
        model.Transform = OtkMatrix4.CreateScale(scale);
        return model;
    }
}
