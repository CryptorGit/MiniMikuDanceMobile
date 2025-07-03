using System.Linq;
using OpenTK.Mathematics;
using SharpGLTF.Schema2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ViewerApp;

internal class VrmModel
{
    public float[] Vertices = Array.Empty<float>();
    public uint[] Indices = Array.Empty<uint>();
    public byte[]? Texture;
    public int TextureWidth;
    public int TextureHeight;
    public Matrix4 Transform = Matrix4.Identity;
}

internal static class VrmLoader
{
    public static VrmModel Load(string path)
    {
        var model = ModelRoot.Load(path);
        var prim = model.LogicalMeshes.First().Primitives.First();
        var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
        var indices = prim.IndexAccessor.AsIndicesArray();

        float[] verts = new float[positions.Count * 3];
        for (int i = 0; i < positions.Count; i++)
        {
            var v = positions[i];
            verts[i * 3 + 0] = v.X;
            verts[i * 3 + 1] = v.Y;
            verts[i * 3 + 2] = v.Z;
        }

        uint[] idx = new uint[indices.Count];
        for (int i = 0; i < indices.Count; i++)
        {
            idx[i] = (uint)indices[i];
        }

        byte[]? texBytes = null;
        int texW = 0;
        int texH = 0;
        var image = prim.Material?.FindChannel("BaseColor")?.Texture?.PrimaryImage
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

        Matrix4 transform = Matrix4.Identity;
        var node = model.DefaultScene?.VisualChildren.FirstOrDefault();
        if (node != null)
        {
            var m = node.WorldMatrix;
            transform = new Matrix4(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44);
        }

        return new VrmModel
        {
            Vertices = verts,
            Indices = idx,
            Texture = texBytes,
            TextureWidth = texW,
            TextureHeight = texH,
            Transform = transform
        };
    }
}
