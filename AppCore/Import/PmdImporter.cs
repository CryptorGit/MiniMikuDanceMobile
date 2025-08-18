using System;
using System.IO;
using MiniMikuDance.App;

namespace MiniMikuDance.Import;

public class PmdImporter : IModelImporter
{
    public float Scale { get; set; } = AppSettings.DefaultModelScale;

    public void Dispose()
    {
    }

    public ModelData ImportModel(Stream stream, string? textureDir = null)
    {
        throw new NotSupportedException("PMD format is not supported yet.");
    }

    public ModelData ImportModel(string path)
    {
        using var fs = File.OpenRead(path);
        return ImportModel(fs, Path.GetDirectoryName(path));
    }
}
