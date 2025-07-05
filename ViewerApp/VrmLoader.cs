using System.Linq;
using System.Collections.Generic;
using OpenTK.Mathematics;
using System.Numerics;
using SharpGLTF.Schema2;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;

namespace ViewerApp;

internal class VrmSubMesh
{
    public float[] Positions = Array.Empty<float>();
    public float[] Normals = Array.Empty<float>();
    public float[] TexCoords = Array.Empty<float>();
    public uint[] Indices = Array.Empty<uint>();
    public byte[]? Texture;
    public int TextureWidth;
    public int TextureHeight;
}

internal class VrmModel
{
    public List<VrmSubMesh> SubMeshes { get; } = new();
    public Matrix4 Transform = Matrix4.Identity;
}

internal static class VrmLoader
{
    public static VrmModel Load(string path)
    {
        var model = ModelRoot.Load(path);
        var result = new VrmModel();

        foreach (var mesh in model.LogicalMeshes)
        {
            foreach (var prim in mesh.Primitives)
            {
                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                var normals = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var channel = prim.Material?.FindChannel("BaseColor");
                var texInfo = channel?.Texture;
                int texCoordIndex = channel?.TextureTransform?.TextureCoordinateOverride ?? channel?.TextureCoordinate ?? 0;
                var uvs = prim.GetVertexAccessor($"TEXCOORD_{texCoordIndex}")?.AsVector2Array();
                var indices = prim.IndexAccessor.AsIndicesArray();

                float[] verts = new float[positions.Count * 3];
                float[] norms = new float[normals?.Count * 3 ?? 0];
                float[] tex = new float[uvs?.Count * 2 ?? 0];
                for (int i = 0; i < positions.Count; i++)
                {
                    var v = positions[i];
                    verts[i * 3 + 0] = v.X;
                    verts[i * 3 + 1] = v.Y;
                    // glTF(+Z forward) を OpenGL(-Z forward) に合わせるため Z を反転
                    verts[i * 3 + 2] = -v.Z;
                }

                if (normals != null)
                {
                    for (int i = 0; i < normals.Count; i++)
                    {
                        var n = normals[i];
                        norms[i * 3 + 0] = n.X;
                        norms[i * 3 + 1] = n.Y;
                        // Z を反転
                        norms[i * 3 + 2] = -n.Z;
                    }
                }

                if (uvs != null)
                {
                    var tt = channel?.TextureTransform;
                    var mat = System.Numerics.Matrix3x2.Identity;
                    if (tt != null)
                    {
                        mat = System.Numerics.Matrix3x2.CreateScale(tt.Scale)
                            * System.Numerics.Matrix3x2.CreateRotation(tt.Rotation)
                            * System.Numerics.Matrix3x2.CreateTranslation(tt.Offset);
                    }
                    for (int i = 0; i < uvs.Count; i++)
                    {
                        var uv = System.Numerics.Vector2.Transform(uvs[i], mat);
                        tex[i * 2 + 0] = uv.X;
                        // ImageSharp は上端原点なので V を反転
                        tex[i * 2 + 1] = 1.0f - uv.Y;
                    }
                }

                uint[] idx = new uint[indices.Count];
                // Z 軸を反転したため頂点順序も入れ替える
                for (int i = 0; i < indices.Count; i += 3)
                {
                    idx[i] = (uint)indices[i];
                    idx[i + 1] = (uint)indices[i + 2];
                    idx[i + 2] = (uint)indices[i + 1];
                }

                byte[]? texBytes = null;
                int texW = 0;
                int texH = 0;
                var image = prim.Material?.FindChannel("BaseColor")?.Texture?.PrimaryImage;
                if (image != null)
                {
                    using var stream = image.OpenImageFile();
                    using var img = ImageSharpImage.Load<Rgba32>(stream);
                    texW = img.Width;
                    texH = img.Height;
                    texBytes = new byte[texW * texH * 4];
                    img.CopyPixelDataTo(texBytes);
                }

                result.SubMeshes.Add(new VrmSubMesh
                {
                    Positions = verts,
                    Normals = norms,
                    TexCoords = tex,
                    Indices = idx,
                    Texture = texBytes,
                    TextureWidth = texW,
                    TextureHeight = texH
                });
            }
        }

        var node = model.DefaultScene?.VisualChildren.FirstOrDefault();
        if (node != null)
        {
            var m = node.WorldMatrix;
            result.Transform = new Matrix4(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44);
        }

        return result;
    }
}
