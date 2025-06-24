using System.IO;
using System.Numerics;                   // Matrix4x4 �p
using MMDTools;                          // PMXParser, PMXObject, Vertex
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;                  // ToGltf2() �̖��O���
using SharpGLTF.Schema2;                 // ModelRoot �p

// �����܂�������G�C���A�X
using SysVector3 = System.Numerics.Vector3;
using MmdVector3 = MMDTools.Vector3;

namespace MiniMikuDance.Import
{
    public static class PmxToGltfService
    {
        /// <summary>
        /// PMX �X�g���[�� �� GLB (byte[]) �ϊ�
        /// </summary>
        public static byte[] Convert(Stream pmxStream)
        {
            // 1) PMX �ǂݍ���
            PMXObject pmx = PMXParser.Parse(pmxStream);

            // 2) ReadOnlyMemory��Span �ŃC���f�N�T���g����悤��
            var verts = pmx.VertexList.Span;   // MMDTools.Vertex[]
            var faces = pmx.SurfaceList.Span;  // (V1,V2,V3)

            // 3) MeshBuilder �ɒ��_����
            var mesh = new MeshBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>("pmx");
            var prim = mesh.UsePrimitive(new MaterialBuilder());

            foreach (var f in faces)
            {
                prim.AddTriangle(
                    ToVPN(verts[f.V1]),
                    ToVPN(verts[f.V2]),
                    ToVPN(verts[f.V3]));
            }

            // 4) SceneBuilder��glTF
            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            // �g�����\�b�h���Ăяo��
            ModelRoot model = scene.ToGltf2();
            return model.WriteGLB().ToArray();
        }

        // MMDTools.Vertex �� SharpGLTF �p���_�^
        static VertexPositionNormal ToVPN(MMDTools.Vertex v) => new(
            new SysVector3((float)v.Position.X, (float)v.Position.Y, (float)v.Position.Z),
            new SysVector3((float)v.Normal.X, (float)v.Normal.Y, (float)v.Normal.Z)
        );
    }
}
