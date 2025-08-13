using System;
using System.Collections.Generic;
using System.IO;
using System.Buffers;
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

internal sealed class TextureData
{
    public int Width;
    public int Height;
    public byte[] Pixels = Array.Empty<byte>();
    public int BufferSize;
}

internal static class PmxLoader
{
    public const float DefaultScale = 0.1f;
    public static int MaxTextureCache { get; set; } = 32;
    private static readonly Dictionary<string, TextureData> s_textureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly LinkedList<string> s_textureLru = new();
    private static readonly Dictionary<string, LinkedListNode<string>> s_textureNodes = new(StringComparer.OrdinalIgnoreCase);

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
                    if (!s_textureCache.TryGetValue(texPath, out var tex))
                    {
                        using var image = Image.Load<Rgba32>(texPath);
                        int size = image.Width * image.Height * 4;
                        var pixels = ArrayPool<byte>.Shared.Rent(size);
                        image.CopyPixelDataTo(pixels.AsSpan(0, size));
                        tex = new TextureData
                        {
                            Width = image.Width,
                            Height = image.Height,
                            Pixels = pixels,
                            BufferSize = pixels.Length
                        };
                        s_textureCache[texPath] = tex;
                        TouchTexture(texPath);
                        TrimCache();
                    }
                    else
                    {
                        TouchTexture(texPath);
                    }
                    sm.TextureWidth = tex.Width;
                    sm.TextureHeight = tex.Height;
                    sm.TextureBytes = tex.Pixels;
                }
            }
            model.SubMeshes.Add(sm);
            faceOffset += faceCount;
        }
        model.Transform = OtkMatrix4.CreateScale(scale);
        return model;
    }

    private static void TouchTexture(string key)
    {
        if (s_textureNodes.TryGetValue(key, out var node))
        {
            s_textureLru.Remove(node);
            s_textureLru.AddLast(node);
        }
        else
        {
            var newNode = s_textureLru.AddLast(key);
            s_textureNodes[key] = newNode;
        }
    }

    private static void TrimCache()
    {
        while (s_textureCache.Count > MaxTextureCache && s_textureLru.First is { } first)
        {
            var oldKey = first.Value;
            s_textureLru.RemoveFirst();
            s_textureNodes.Remove(oldKey);
            if (s_textureCache.TryGetValue(oldKey, out var tex))
            {
                ArrayPool<byte>.Shared.Return(tex.Pixels);
                tex.Pixels = Array.Empty<byte>();
                tex.BufferSize = 0;
                s_textureCache.Remove(oldKey);
            }
        }
    }
}
