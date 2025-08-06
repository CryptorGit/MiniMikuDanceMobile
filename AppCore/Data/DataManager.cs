using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniMikuDance.Data;

public partial class DataManager : Util.Singleton<DataManager>
{
    public async Task<T> LoadConfigAsync<T>(string key) where T : new()
    {
        var path = Path.Combine("Configs", $"{key}.json");
        var stream = await OpenPackageFileAsync(path);
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
                    return await JsonSerializer.DeserializeAsync<T>(stream, opts) ?? new T();
                }
                catch
                {
                    return new T();
                }
            }
        }

        return await Task.Run(() => Util.JSONUtil.Load<T>(path));
    }

    /// <summary>
    /// プラットフォーム依存のパッケージファイル読み込み関数。
    /// </summary>
    public static Func<string, Task<Stream?>> OpenPackageFileFunc { get; set; } = path =>
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, path);
        Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    };

    private Task<Stream?> OpenPackageFileAsync(string path)
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
