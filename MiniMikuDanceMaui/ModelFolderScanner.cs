using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MiniMikuDanceMaui;

public static class ModelFolderScanner
{
    private static readonly string[] TextureExtensions = { ".png", ".jpg", ".jpeg", ".tga" };

    public static (string? modelPath, List<string> texturePaths) Scan(string directory)
    {
        if (!Directory.Exists(directory))
            return (null, new List<string>());

        var model = Directory.EnumerateFiles(directory, "*.pmx").FirstOrDefault()
                    ?? Directory.EnumerateFiles(directory, "*.pmd").FirstOrDefault();

        var textures = new List<string>();
        // root textures
        textures.AddRange(EnumerateTextures(directory));
        foreach (var sub in new[] { "tex", "spa", "sph" })
        {
            var subDir = Path.Combine(directory, sub);
            if (Directory.Exists(subDir))
                textures.AddRange(EnumerateTextures(subDir));
        }

        // convert to relative paths
        var rel = textures.Select(p => Path.GetRelativePath(directory, p)).ToList();
        return (model, rel);
    }

    private static IEnumerable<string> EnumerateTextures(string dir)
    {
        return Directory.EnumerateFiles(dir)
            .Where(f => TextureExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f);
    }
}
