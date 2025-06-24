using PMXParser;
using SharpGLTF.Scenes;
using System.Numerics;
using SharpGLTF.Geometry.VertexTypes;

namespace MiniMikuDance.Import;

public static class PmxToGltfService
{
    public static byte[] Convert(Stream pmx)
    {
        var pmxModel = PMXModel.FromStream(pmx);
        var scene = new SceneBuilder();
        // TODO: convert vertices and skins properly
        foreach (var v in pmxModel.VertexList)
        {
            var vb = new SharpGLTF.Geometry.VertexBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>();
            vb.Geometry.Position = new System.Numerics.Vector3(v.Position.X, v.Position.Y, v.Position.Z);
            vb.Geometry.Normal = new System.Numerics.Vector3(v.Normal.X, v.Normal.Y, v.Normal.Z);
            scene.AddRigidMesh(vb, Matrix4x4.Identity);
        }
        using var glb = scene.ToGltfBinary();
        return glb.ToArray();
    }
}
