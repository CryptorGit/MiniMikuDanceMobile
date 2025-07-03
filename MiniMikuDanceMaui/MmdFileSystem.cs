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
        var path = SystemPath.Combine(root, "MiniMikuDance", "data");
        try
        {
            Directory.CreateDirectory(path);
            BaseDir = path;
        }
        catch
        {
            // 権限不足などで外部ストレージへ書き込めない場合は
            // アプリ専用のデータフォルダを使用する
            root = FileSystem.AppDataDirectory;
            path = SystemPath.Combine(root, "MiniMikuDance", "data");
            Directory.CreateDirectory(path);
            BaseDir = path;
        }
#else
        var root = FileSystem.AppDataDirectory;
        BaseDir = SystemPath.Combine(root, "MiniMikuDance", "data");
        Directory.CreateDirectory(BaseDir);
#endif
    }

    public static string Ensure(string subdir)
    {
        var path = SystemPath.Combine(BaseDir, subdir);
        Directory.CreateDirectory(path);
        return path;
    }

    public static void AppendAccessLog(string directory)
    {
        try
        {
            var logPath = SystemPath.Combine(directory, "log.txt");
            File.AppendAllText(logPath,
                $"Connected: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC{Environment.NewLine}");
        }
        catch
        {
            // ignore logging failures
        }
    }
}
