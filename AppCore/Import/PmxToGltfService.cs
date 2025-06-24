using MMDTools;
using SharpGLTF.Scenes;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using System.Numerics;
using System.IO;

using SysVector3 = System.Numerics.Vector3;

namespace MiniMikuDance.Import;

public static class PmxToGltfService
{
    public static byte[] Convert(Stream pmxStream)
    {
        var pmx = PMXParser.Parse(pmxStream);

        var mesh = new MeshBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>("pmx");
        var prim = mesh.UsePrimitive(new MaterialBuilder());

        var faces = pmx.SurfaceList.Span;
        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            var v0 = pmx.VertexList[face.V1];
            var v1 = pmx.VertexList[face.V2];
            var v2 = pmx.VertexList[face.V3];

            var a = new VertexPositionNormal(new SysVector3((float)v0.Position.X, (float)v0.Position.Y, (float)v0.Position.Z),
                                             new SysVector3((float)v0.Normal.X, (float)v0.Normal.Y, (float)v0.Normal.Z));
            var b = new VertexPositionNormal(new SysVector3((float)v1.Position.X, (float)v1.Position.Y, (float)v1.Position.Z),
                                             new SysVector3((float)v1.Normal.X, (float)v1.Normal.Y, (float)v1.Normal.Z));
            var c = new VertexPositionNormal(new SysVector3((float)v2.Position.X, (float)v2.Position.Y, (float)v2.Position.Z),
                                             new SysVector3((float)v2.Normal.X, (float)v2.Normal.Y, (float)v2.Normal.Z));

            prim.AddTriangle(a, b, c);
        }

        var scene = new SceneBuilder();
        scene.AddRigidMesh(mesh, Matrix4x4.Identity);

        using var model = scene.ToGltf2();
        return model.WriteGLB().ToArray();
    }
}
