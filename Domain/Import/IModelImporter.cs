using System;
using System.IO;

namespace MiniMikuDance.Import;

public interface IModelImporter : IDisposable
{
    float Scale { get; set; }
    ModelData ImportModel(Stream stream, string? textureDir = null);
    ModelData ImportModel(string path);
}
