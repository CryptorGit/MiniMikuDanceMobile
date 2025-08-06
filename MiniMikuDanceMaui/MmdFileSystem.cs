using System.IO;
using SystemPath = System.IO.Path;
using Microsoft.Maui.Storage;

namespace MiniMikuDanceMaui;

public static class MmdFileSystem
{
    public static readonly string BaseDir;

    static MmdFileSystem()
    {
        var root = FileSystem.AppDataDirectory;
        BaseDir = SystemPath.Combine(root, "MiniMikuDance", "data");
        Directory.CreateDirectory(BaseDir);
        Directory.CreateDirectory(SystemPath.Combine(BaseDir, "Movie"));
        LogService.WriteLine($"[MmdFileSystem] Using storage: {BaseDir}", LogService.LogLevel.Info);
    }

    public static string Ensure(string subdir)
    {
        var path = SystemPath.Combine(BaseDir, subdir);
        Directory.CreateDirectory(path);
        return path;
    }
}
