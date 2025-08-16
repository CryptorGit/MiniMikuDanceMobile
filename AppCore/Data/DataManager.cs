using System;
using System.IO;

namespace MiniMikuDance.Data;

public partial class DataManager : Util.Singleton<DataManager>
{
    public T LoadConfig<T>(string key) where T : new()
    {
        if (!Directory.Exists("Configs"))
            Directory.CreateDirectory("Configs");
        var path = Path.Combine("Configs", $"{key}.json");
        var stream = OpenPackageFile(path);
        if (stream != null)
        {
            using (stream)
            {
                return Util.JSONUtil.LoadFromStream<T>(stream);
            }
        }

        if (!File.Exists(path))
        {
            var src = Path.Combine(AppContext.BaseDirectory, path);
            if (File.Exists(src))
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.Copy(src, path);
            }
        }
        return Util.JSONUtil.Load<T>(path);
    }

    /// <summary>
    /// プラットフォーム依存のパッケージファイル読み込み関数。
    /// </summary>
    public static Func<string, Stream?>? OpenPackageFileFunc { get; set; }

    private Stream? OpenPackageFile(string path)
        => OpenPackageFileFunc?.Invoke(path);

    public void SaveConfig<T>(string key, T data)
    {
        if (!Directory.Exists("Configs"))
            Directory.CreateDirectory("Configs");
        Util.JSONUtil.Save(Path.Combine("Configs", $"{key}.json"), data);
    }

    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "MiniMikuDance_Temp");

    public string TempDir => _tempDir;

    public void CleanupTemp()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        Directory.CreateDirectory(_tempDir);
    }
}

public partial class DataManager
{
    // 非 MAUI 環境向け既定実装
    static DataManager()
    {
        OpenPackageFileFunc = _ => null;
    }
}
