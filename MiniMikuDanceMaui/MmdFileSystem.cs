using System.IO;
using SystemPath = System.IO.Path;
using Microsoft.Maui.Storage;

namespace MiniMikuDanceMaui;

public static class MmdFileSystem
{
    public static readonly string BaseDir = SystemPath.Combine(FileSystem.AppDataDirectory, "MiniMikuDance");

    static MmdFileSystem()
    {
        Directory.CreateDirectory(BaseDir);
    }

    public static string Ensure(string subdir)
    {
        var path = SystemPath.Combine(BaseDir, subdir);
        Directory.CreateDirectory(path);
        return path;
    }
}
