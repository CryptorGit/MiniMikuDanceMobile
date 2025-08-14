using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiniMikuDance.Import;

public class ModelData
{
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public int VertexCount { get; set; }
    public List<BoneData> Bones { get; } = new();
    public List<MorphData> Morphs { get; } = new();
}

public class ModelImporter
{
    private readonly ILogger<ModelImporter> _logger;

    public static int CacheCapacity { get; set; }
    public static void ClearCache() { }

    public float Scale { get; set; } = AppSettings.DefaultModelScale;

    public ModelImporter(ILogger<ModelImporter>? logger = null)
    {
        _logger = logger ?? NullLogger<ModelImporter>.Instance;
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

        uint boneCount = Nanoem.ModelGetBoneCount(model);
        for (uint i = 0; i < boneCount; i++)
        {
            var b = Nanoem.ModelGetBoneInfo(model, i);
            data.Bones.Add(new BoneData
            {
                Name = Nanoem.PtrToStringAndFree(b.Name),
                Parent = b.ParentBoneIndex,
                Translation = new Vector3(b.OriginX * Scale, b.OriginY * Scale, b.OriginZ * Scale)
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
                Type = (MorphType)m.Type
            });
        }

        Nanoem.ModelDestroy(model);
        return data;
    }
}
