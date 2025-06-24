using MMDTools;
using SharpGLTF.Scenes;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using System.Numerics;
using System.IO;

namespace MiniMikuDance.Import;

public static class PmxToGltfService
{
    public static byte[] Convert(Stream pmxStream)
    {
        var pmx = PMXParser.Parse(pmxStream);

        var mesh = new MeshBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>("pmx");

        var vertices = pmx.VertexList.Span;
        var faces = pmx.SurfaceList.Span;

        foreach (var f in faces)
        {
            var v0 = vertices[f.V1];
            var v1 = vertices[f.V2];
            var v2 = vertices[f.V3];

            var a = new VertexPositionNormal(new Vector3(v0.Position.X, v0.Position.Y, v0.Position.Z),
                                             new Vector3(v0.Normal.X, v0.Normal.Y, v0.Normal.Z));
            var b = new VertexPositionNormal(new Vector3(v1.Position.X, v1.Position.Y, v1.Position.Z),
                                             new Vector3(v1.Normal.X, v1.Normal.Y, v1.Normal.Z));
            var c = new VertexPositionNormal(new Vector3(v2.Position.X, v2.Position.Y, v2.Position.Z),
                                             new Vector3(v2.Normal.X, v2.Normal.Y, v2.Normal.Z));

            mesh.AddTriangle(a, b, c);
        }

        var scene = new SceneBuilder();
        scene.AddRigidMesh(mesh, Matrix4x4.Identity);

        using var model = scene.ToGltf2();
        return model.WriteGLB().ToArray();
    }
}
