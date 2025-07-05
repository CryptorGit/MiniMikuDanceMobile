using System.Linq;
using System.Collections.Generic;
using System.IO;
using OpenTK.Mathematics;
using System.Numerics;
using SharpGLTF.Schema2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ViewerApp;

internal class VrmSubMesh
{
    public float[] Positions = Array.Empty<float>();
    public float[] Normals = Array.Empty<float>();
    public float[] TexCoords = Array.Empty<float>();
    public uint[] Indices = Array.Empty<uint>();
    public OpenTK.Mathematics.Vector4 ColorFactor = OpenTK.Mathematics.Vector4.One;
    public byte[]? TextureBytes;
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
        var bytes = File.ReadAllBytes(path);
        using var ms = new MemoryStream(bytes);
        var model = ModelRoot.ReadGLB(ms);
        var texMap = VrmUtil.ReadMainTextureIndices(bytes);
        var result = new VrmModel();

        foreach (var mesh in model.LogicalMeshes)
        {
            foreach (var prim in mesh.Primitives)
            {
                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                var normals = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var tex = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                var channel = prim.Material?.FindChannel("BaseColor");
                var indices = prim.IndexAccessor.AsIndicesArray();

                float[] verts = new float[positions.Count * 3];
                float[] norms = new float[normals?.Count * 3 ?? 0];
                float[] uvs = new float[tex?.Count * 2 ?? 0];
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

                if (tex != null)
                {
                    for (int i = 0; i < tex.Count; i++)
                    {
                        var uv = tex[i];
                        uvs[i * 2 + 0] = uv.X;
                        uvs[i * 2 + 1] = uv.Y;
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

                var cf = channel?.Parameter ?? new System.Numerics.Vector4(1, 1, 1, 1);
                // マテリアル側のアルファ値は利用せず常に不透明で描画
                cf.W = 1.0f;
                var colorFactor = new OpenTK.Mathematics.Vector4(cf.X, cf.Y, cf.Z, cf.W);

                var smd = new VrmSubMesh
                {
                    Positions = verts,
                    Normals = norms,
                    TexCoords = uvs,
                    Indices = idx,
                    ColorFactor = colorFactor
                };

                int matIndex = prim.Material?.LogicalIndex ?? -1;
                if (matIndex >= 0 && texMap.TryGetValue(matIndex, out var tIdx))
                {
                    var texObj = model.LogicalTextures[tIdx];
                    var img = texObj.PrimaryImage?.Content;
                    if (img != null)
                    {
                        using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(img.Content.ToArray());
                        smd.TextureWidth = image.Width;
                        smd.TextureHeight = image.Height;
                        smd.TextureBytes = new byte[image.Width * image.Height * 4];
                        image.CopyPixelDataTo(smd.TextureBytes);
                    }
                }

                result.SubMeshes.Add(smd);
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
