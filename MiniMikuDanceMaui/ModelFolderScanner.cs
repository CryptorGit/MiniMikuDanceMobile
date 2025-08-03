using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MiniMikuDanceMaui;

public static class ModelFolderScanner
{
    private static readonly string[] TextureExtensions = { ".png", ".jpg", ".jpeg", ".tga" };

    public static (List<string> modelPaths, Dictionary<string, string> texturePathMap) Scan(string directory)
    {
        if (!Directory.Exists(directory))
            return (new List<string>(), new Dictionary<string, string>());

        var models = Directory.EnumerateFiles(directory)
            .Where(f => f.EndsWith(".pmx", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".pmd", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();

        var textures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // root textures
        AddTextures(directory, directory, textures);
        foreach (var sub in new[] { "tex", "spa", "sph" })
        {
            var subDir = Path.Combine(directory, sub);
            if (Directory.Exists(subDir))
                AddTextures(subDir, directory, textures);
        }

        return (models, textures);
    }

    private static void AddTextures(string dir, string root, Dictionary<string, string> map)
    {
        foreach (var file in EnumerateTextures(dir))
        {
            var name = Path.GetFileName(file);
            var rel = Path.GetRelativePath(root, file);
            map.TryAdd(name, rel);
        }
    }

    private static IEnumerable<string> EnumerateTextures(string dir)
    {
        return Directory.EnumerateFiles(dir)
            .Where(f => TextureExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f);
    }
}
