using System;
using System.IO;
using SystemPath = System.IO.Path;
using Microsoft.Maui.Storage;

namespace MiniMikuDanceMaui.Services;

public static class FileSystemService
{
    public static readonly string BaseDir;
    public static bool UsingInternalStorage { get; private set; }
    public static bool FallbackToInternalStorage { get; private set; }

    static FileSystemService()
    {
#if ANDROID
        string baseDir;
        try
        {
            var root = Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath;
            if (string.IsNullOrEmpty(root))
                root = FileSystem.AppDataDirectory;
            baseDir = SystemPath.Combine(root, "MiniMikuDance", "data");
            Directory.CreateDirectory(baseDir);
            UsingInternalStorage = false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            // 権限不足などで外部ストレージへ書き込めない場合は
            // アプリ専用のデータフォルダを使用する
            var root = FileSystem.AppDataDirectory;
            baseDir = SystemPath.Combine(root, "MiniMikuDance", "data");
            Directory.CreateDirectory(baseDir);
            UsingInternalStorage = true;
            FallbackToInternalStorage = true;
        }
        BaseDir = baseDir;
#else
        var root = FileSystem.AppDataDirectory;
        BaseDir = SystemPath.Combine(root, "MiniMikuDance", "data");
        Directory.CreateDirectory(BaseDir);
        UsingInternalStorage = true;
#endif
        Directory.CreateDirectory(SystemPath.Combine(BaseDir, "Movie"));
    }

    public static string Ensure(string subdir)
    {
        var path = SystemPath.Combine(BaseDir, subdir);
        Directory.CreateDirectory(path);
        return path;
    }
}
