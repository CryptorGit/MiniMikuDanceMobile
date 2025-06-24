using System.IO;
using System.Numerics;
using MMDTools;                       // �� PMXParser, PMXObject, Vertex ������
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;              // �� ToGltf2() �g�����\�b�h
using SharpGLTF.Schema2;
using SysVector3 = System.Numerics.Vector3;

namespace MiniMikuDance.Import;

public static class PmxToGltfService
{
    /// <summary>
    /// PMX �o�C�i���X�g���[���� glTF �o�C�i��(GLB) �� byte[] �֕ϊ�
    /// </summary>
    public static byte[] Convert(Stream pmxStream)
    {
        // 1) PMX ���p�[�X
        PMXObject pmx = PMXParser.Parse(pmxStream);

        // 2) ReadOnlyMemory<T> �� Span �ɕϊ����ăC���f�N�T [] ���g��
        var verts = pmx.VertexList.Span;   // MMDTools.Vertex[]
        var faces = pmx.SurfaceList.Span;  // �ʁiV1,V2,V3 �̎O�p�`�j

        // 3) MeshBuilder �֎O�p�`�ǉ�
        var mesh = new MeshBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>("pmx");
        var prim = mesh.UsePrimitive(new MaterialBuilder());

        foreach (var f in faces)
        {
            prim.AddTriangle(
                ToVPN(verts[f.V1]),
                ToVPN(verts[f.V2]),
                ToVPN(verts[f.V3]));
        }

        // 4) SceneBuilder �� glTF
        var scene = new SceneBuilder();
        scene.AddRigidMesh(mesh, Matrix4x4.Identity);

        // ToGltf2() �� SharpGLTF.Scenes �̊g�����\�b�h
        ModelRoot model = scene.ToGltf2();
        return model.WriteGLB().ToArray();  // ArraySegment<byte> �� byte[]
    }

    /// <summary>
    /// MMDTools.Vertex �� SharpGLTF ���_�^�֕ϊ�
    /// </summary>
    private static VertexPositionNormal ToVPN(Vertex v) => new(
        new SysVector3((float)v.Position.X, (float)v.Position.Y, (float)v.Position.Z),
        new SysVector3((float)v.Normal.X, (float)v.Normal.Y, (float)v.Normal.Z));
}
