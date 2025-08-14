using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MiniMikuDance.App;

namespace MiniMikuDance.Import;

    public class FaceData
    {
        public int[] Indices { get; set; } = Array.Empty<int>();
    }

    public class MeshData
    {
        public List<Vector3> Vertices { get; } = new();
        public List<Vector3> Normals { get; } = new();
        public List<Vector2> TexCoords { get; } = new();
        public List<int> Indices { get; } = new();
        public List<Vector4> JointIndices { get; } = new();
        public List<Vector4> JointWeights { get; } = new();
        public List<FaceData> Faces { get; } = new();
        public int VertexCount => Vertices.Count;
    }

    public class SubMeshData
    {
        public MeshData Mesh { get; set; } = new();
        public List<Vector2> TexCoords { get; } = new();
        public List<Vector4> JointIndices { get; } = new();
        public List<Vector4> JointWeights { get; } = new();
        public List<Vector3> SdefC { get; } = new();
        public List<Vector3> SdefR0 { get; } = new();
        public List<Vector3> SdefR1 { get; } = new();
        public string TextureFilePath { get; set; } = string.Empty;
    }

    public class TransformData
    {
        public Matrix4x4 ToMatrix4() => Matrix4x4.Identity;
    }

    public class ModelData
    {
        public string Name { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public int VertexCount { get; set; }
        public List<BoneData> Bones { get; } = new();
        public List<MorphData> Morphs { get; } = new();
        public List<string> Textures { get; } = new();
        public List<MaterialData> Materials { get; } = new();
        public MeshData Mesh { get; set; } = new();
        public List<SubMeshData> SubMeshes { get; } = new();
        public List<RigidBodyData> RigidBodies { get; } = new();
        public List<JointData> Joints { get; } = new();
        public Dictionary<string, int> HumanoidBoneList { get; } = new();
        public TransformData Transform { get; set; } = new();
        public float ShadeShift { get; set; }
        public float ShadeToony { get; set; }
        public float RimIntensity { get; set; }
    }

public partial class ModelImporter : IDisposable
{
    private readonly ILogger<ModelImporter> _logger;

    public static int CacheCapacity { get; set; }

    public float Scale { get; set; } = AppSettings.DefaultModelScale;

    public ModelImporter(ILogger<ModelImporter>? logger = null)
    {
        _logger = logger ?? NullLogger<ModelImporter>.Instance;
    }

    public void Dispose()
    {
    }

    public ModelData ImportModel(string path)
    {
        using var fs = File.OpenRead(path);
        return ImportModel(fs);
    }

    public ModelData ImportModel(Stream stream, string? baseDir)
    {
        return ImportModel(stream);
    }

    public ModelData ImportModel(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        int status;
        IntPtr model = Nanoem.ModelImportPmx(bytes, out status);
        if (model == IntPtr.Zero || status != 0)
        {
            throw new InvalidOperationException($"PMX import failed: {status}");
        }

        var data = new ModelData();
        var info = Nanoem.ModelGetInfo(model);
        data.Name = Nanoem.PtrToStringAndFree(info.Name);
        data.EnglishName = Nanoem.PtrToStringAndFree(info.EnglishName);
        data.VertexCount = (int)Nanoem.ModelGetVertexCount(model);

        uint textureCount = Nanoem.ModelGetTextureCount(model);
        for (uint i = 0; i < textureCount; i++)
        {
            data.Textures.Add(Nanoem.ModelGetTexturePath(model, i));
        }

        uint materialCount = Nanoem.ModelGetMaterialCount(model);
        for (uint i = 0; i < materialCount; i++)
        {
            var m = Nanoem.ModelGetMaterialInfo(model, i);
            data.Materials.Add(new MaterialData
            {
                Name = Nanoem.PtrToStringAndFree(m.Name),
                EnglishName = Nanoem.PtrToStringAndFree(m.EnglishName),
                Diffuse = new Vector4(m.DiffuseR, m.DiffuseG, m.DiffuseB, m.DiffuseA),
                Specular = new Vector4(m.SpecularR, m.SpecularG, m.SpecularB, m.SpecularA),
                Ambient = new Vector4(m.AmbientR, m.AmbientG, m.AmbientB, m.AmbientA),
                TextureIndex = m.TextureIndex
            });
        }

        uint boneCount = Nanoem.ModelGetBoneCount(model);
        for (uint i = 0; i < boneCount; i++)
        {
            var b = Nanoem.ModelGetBoneInfo(model, i);
            var t = new Vector3(b.OriginX * Scale, b.OriginY * Scale, b.OriginZ * Scale);
            var bind = Matrix4x4.CreateTranslation(t);
            Matrix4x4.Invert(bind, out var invBind);
            data.Bones.Add(new BoneData
            {
                Name = Nanoem.PtrToStringAndFree(b.Name),
                Parent = b.ParentBoneIndex,
                Translation = t,
                BindMatrix = bind,
                InverseBindMatrix = invBind,
                Transform = bind
            });
        }

        uint morphCount = Nanoem.ModelGetMorphCount(model);
        for (uint i = 0; i < morphCount; i++)
        {
            var m = Nanoem.ModelGetMorphInfo(model, i);
            data.Morphs.Add(new MorphData
            {
                Index = (int)i,
                Name = Nanoem.PtrToStringAndFree(m.Name),
                Category = (MorphCategory)m.Category,
                Type = (MorphType)m.Type,
                DefaultWeight = Nanoem.ModelGetMorphInitialWeight(model, i)
            });
        }

        LoadMesh(model, data);
        Nanoem.ModelDestroy(model);
        return data;
    }
}
