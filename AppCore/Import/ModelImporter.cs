using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiniMikuDance.Import;

public class ModelData
{
    public List<SubMeshData> SubMeshes { get; set; } = new();
    public MeshData Mesh { get; set; } = new();
    public System.Numerics.Matrix4x4 Transform { get; set; } = System.Numerics.Matrix4x4.Identity;
    public List<BoneData> Bones { get; set; } = new();
    public Dictionary<string, int> HumanoidBones { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(string Name, int Index)> HumanoidBoneList { get; set; } = new();
    public List<MorphData> Morphs { get; set; } = new();
    public List<RigidBodyData> RigidBodies { get; set; } = new();
    public List<JointData> Joints { get; set; } = new();
    public float ShadeShift { get; set; } = -0.1f;
    public float ShadeToony { get; set; } = 0.9f;
    public float RimIntensity { get; set; } = 0.5f;
}

public class ModelImporter
{
    private readonly ILogger<ModelImporter> _logger;
    public float Scale { get; set; } = AppSettings.DefaultModelScale;

    public ModelImporter(ILogger<ModelImporter>? logger = null)
    {
        _logger = logger ?? NullLogger<ModelImporter>.Instance;
    }

    public ModelData ImportModel(Stream stream, string? textureDir = null)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var data = ms.ToArray();
        // TODO: ネイティブインポートから詳細データを取得する
        int vertexCount = NativeModelImporter.GetVertexCount(data);
        var model = new ModelData();
        for (int i = 0; i < vertexCount; i++)
        {
            model.Mesh.Vertices.Add(System.Numerics.Vector3.Zero);
        }
        return model;
    }
}
