using System.IO;
using System.Text.Json;

namespace MiniMikuDance.Data;

public partial class DataManager : Util.Singleton<DataManager>
{
    public T LoadConfig<T>(string key) where T : new()
    {
        var path = $"Configs/{key}.json";
        var stream = OpenPackageFile(path);
        if (stream != null)
        {
            using (stream)
            {
                try
                {
                    var opts = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    opts.Converters.Add(new Util.Vector3JsonConverter());
                    return JsonSerializer.Deserialize<T>(stream, opts) ?? new T();
                }
                catch
                {
                    return new T();
                }
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
                try
                {
                    File.Copy(src, path);
                }
                catch
                {
                    // ignore copy failures
                }
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
