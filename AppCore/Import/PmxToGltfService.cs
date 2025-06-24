using System.IO;
using System.Numerics;
using MMDTools;                       // ← PMXParser, PMXObject, Vertex がここ
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;              // ← ToGltf2() 拡張メソッド
using SharpGLTF.Schema2;
using SysVector3 = System.Numerics.Vector3;

namespace MiniMikuDance.Import;

public static class PmxToGltfService
{
    /// <summary>
    /// PMX バイナリストリームを glTF バイナリ(GLB) の byte[] へ変換
    /// </summary>
    public static byte[] Convert(Stream pmxStream)
    {
        // 1) PMX をパース
        PMXObject pmx = PMXParser.Parse(pmxStream);

        // 2) ReadOnlyMemory<T> → Span に変換してインデクサ [] を使う
        var verts = pmx.VertexList.Span;   // MMDTools.Vertex[]
        var faces = pmx.SurfaceList.Span;  // 面（V1,V2,V3 の三角形）

        // 3) MeshBuilder へ三角形追加
        var mesh = new MeshBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>("pmx");
        var prim = mesh.UsePrimitive(new MaterialBuilder());

        foreach (var f in faces)
        {
            prim.AddTriangle(
                ToVPN(verts[f.V1]),
                ToVPN(verts[f.V2]),
                ToVPN(verts[f.V3]));
        }

        // 4) SceneBuilder → glTF
        var scene = new SceneBuilder();
        scene.AddRigidMesh(mesh, Matrix4x4.Identity);

        // ToGltf2() は SharpGLTF.Scenes の拡張メソッド
        ModelRoot model = scene.ToGltf2();
        return model.WriteGLB().ToArray();  // ArraySegment<byte> → byte[]
    }

    /// <summary>
    /// MMDTools.Vertex → SharpGLTF 頂点型へ変換
    /// </summary>
    private static VertexPositionNormal ToVPN(Vertex v) => new(
        new SysVector3((float)v.Position.X, (float)v.Position.Y, (float)v.Position.Z),
        new SysVector3((float)v.Normal.X, (float)v.Normal.Y, (float)v.Normal.Z));
}
