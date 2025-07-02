using System.IO;
using SystemPath = System.IO.Path;
using Microsoft.Maui.Storage;

namespace MiniMikuDanceMaui;

public static class MmdFileSystem
{
    public static readonly string BaseDir;

    static MmdFileSystem()
    {
#if ANDROID
        var root = Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath;
        if (string.IsNullOrEmpty(root))
            root = FileSystem.AppDataDirectory;
#else
        var root = FileSystem.AppDataDirectory;
#endif
        BaseDir = SystemPath.Combine(root, "MiniMikuDance", "data");
        Directory.CreateDirectory(BaseDir);
    }

    public static string Ensure(string subdir)
    {
        var path = SystemPath.Combine(BaseDir, subdir);
        Directory.CreateDirectory(path);
        return path;
    }
}
