using System;
using System.Numerics;

namespace MiniMikuDance.Import;

partial class ModelImporter
{
    private void LoadMesh(IntPtr model, ModelData data)
    {
        var vertices = Nanoem.ModelGetVertexBuffer(model);
        foreach (var v in vertices)
        {
            data.Mesh.Vertices.Add(new Vector3(v.PX * Scale, v.PY * Scale, v.PZ * Scale));
            data.Mesh.Normals.Add(new Vector3(v.NX, v.NY, v.NZ));
            data.Mesh.TexCoords.Add(new Vector2(v.U, v.V));
            data.Mesh.JointIndices.Add(new Vector4(v.BoneIndex0, v.BoneIndex1, v.BoneIndex2, v.BoneIndex3));
            data.Mesh.JointWeights.Add(new Vector4(v.Weight0, v.Weight1, v.Weight2, v.Weight3));
        }

        var indices = Nanoem.ModelGetIndexBuffer(model);
        foreach (var i in indices)
        {
            data.Mesh.Indices.Add((int)i);
        }

        if (data.SubMeshes.Count == 0)
        {
            var sub = new SubMeshData { Mesh = data.Mesh };
            sub.TexCoords.AddRange(data.Mesh.TexCoords);
            sub.JointIndices.AddRange(data.Mesh.JointIndices);
            sub.JointWeights.AddRange(data.Mesh.JointWeights);
            data.SubMeshes.Add(sub);
        }
    }
}

