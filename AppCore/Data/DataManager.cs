using System.IO;

namespace MiniMikuDance.Data;

public partial class DataManager : Util.Singleton<DataManager>
{
    public T LoadConfig<T>(string key) where T : new()
    {
        var path = Path.Combine("Configs", $"{key}.json");
        var stream = OpenPackageFile(path);
        if (stream != null)
        {
            using (stream)
            {
                return Util.JSONUtil.LoadFromStream<T>(stream);
            }
        }

        return Util.JSONUtil.Load<T>(path);
    }

    /// <summary>
    /// プラットフォーム依存のパッケージファイル読み込み関数。
    /// </summary>
    public static Func<string, Stream?> OpenPackageFileFunc { get; set; } = path =>
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, path);
        return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
    };

    private Stream? OpenPackageFile(string path)
        => OpenPackageFileFunc(path);

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
