using System;
using System.IO;
using MiniMikuDance.Domain.Interfaces;

namespace MiniMikuDance.Data.Repositories;

public partial class SettingsRepository : Util.Singleton<SettingsRepository>, ISettingsRepository
{
    public T LoadConfig<T>(string key) where T : new()
    {
        var path = $"Configs/{key}.json";
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
        Util.JSONUtil.Save($"Configs/{key}.json", data);
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

public partial class SettingsRepository
{
    // 非 MAUI 環境向け既定実装
    static SettingsRepository()
    {
        OpenPackageFileFunc = _ => null;
    }
}
