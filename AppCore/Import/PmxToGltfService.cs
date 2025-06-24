using System.IO;
using System.Numerics;                   // Matrix4x4 用
using MMDTools;                          // PMXParser, PMXObject, Vertex
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;                  // ToGltf2() の名前空間
using SharpGLTF.Schema2;                 // ModelRoot 用

// あいまいさ回避エイリアス
using SysVector3 = System.Numerics.Vector3;
using MmdVector3 = MMDTools.Vector3;

namespace MiniMikuDance.Import
{
    public static class PmxToGltfService
    {
        /// <summary>
        /// PMX ストリーム → GLB (byte[]) 変換
        /// </summary>
        public static byte[] Convert(Stream pmxStream)
        {
            // 1) PMX 読み込み
            PMXObject pmx = PMXParser.Parse(pmxStream);

            // 2) ReadOnlyMemory→Span でインデクサが使えるように
            var verts = pmx.VertexList.Span;   // MMDTools.Vertex[]
            var faces = pmx.SurfaceList.Span;  // (V1,V2,V3)

            // 3) MeshBuilder に頂点投入
            var mesh = new MeshBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>("pmx");
            var prim = mesh.UsePrimitive(new MaterialBuilder());

            foreach (var f in faces)
            {
                prim.AddTriangle(
                    ToVPN(verts[f.V1]),
                    ToVPN(verts[f.V2]),
                    ToVPN(verts[f.V3]));
            }

            // 4) SceneBuilder→glTF
            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            // 拡張メソッドを呼び出し
            ModelRoot model = scene.ToGltf2();
            return model.WriteGLB().ToArray();
        }

        // MMDTools.Vertex → SharpGLTF 用頂点型
        static VertexPositionNormal ToVPN(MMDTools.Vertex v) => new(
            new SysVector3((float)v.Position.X, (float)v.Position.Y, (float)v.Position.Z),
            new SysVector3((float)v.Normal.X, (float)v.Normal.Y, (float)v.Normal.Z)
        );
    }
}
