using System.IO;
using SystemPath = System.IO.Path;
using Microsoft.Maui.Storage;

namespace MiniMikuDanceMaui;

public static class MmdFileSystem
{
    public static readonly string BaseDir;
    public static bool UsingInternalStorage { get; private set; }

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
            UsingInternalStorage = false;
            LogService.WriteLine($"[MmdFileSystem] Using external storage: {BaseDir}", LogService.LogLevel.Info);
            Directory.CreateDirectory(SystemPath.Combine(BaseDir, "Movie"));
        }
        catch
        {
            // 権限不足などで外部ストレージへ書き込めない場合は
            // アプリ専用のデータフォルダを使用する
            root = FileSystem.AppDataDirectory;
            path = SystemPath.Combine(root, "MiniMikuDance", "data");
            Directory.CreateDirectory(path);
            BaseDir = path;
            UsingInternalStorage = true;
            LogService.WriteLine($"[MmdFileSystem] Fallback to internal storage: {BaseDir}", LogService.LogLevel.Info);
            Directory.CreateDirectory(SystemPath.Combine(BaseDir, "Movie"));
        }
#else
        var root = FileSystem.AppDataDirectory;
        BaseDir = SystemPath.Combine(root, "MiniMikuDance", "data");
        Directory.CreateDirectory(BaseDir);
        UsingInternalStorage = true;
        LogService.WriteLine($"[MmdFileSystem] Using internal storage: {BaseDir}", LogService.LogLevel.Info);
        Directory.CreateDirectory(SystemPath.Combine(BaseDir, "Movie"));
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
                $"Connected: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}");
        }
        catch
        {
            // ignore logging failures
        }
    }
}
