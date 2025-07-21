using System.IO;

namespace MiniMikuDance.Data;

public partial class DataManager : Util.Singleton<DataManager>
{
    public T LoadConfig<T>(string key) where T : new()
    {
        var path = $"Configs/{key}.json";
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
            else
            {
                var stream = OpenPackageFile(path);
                if (stream != null)
                {
                    using (stream)
                    {
                        return Util.JSONUtil.LoadFromStream<T>(stream);
                    }
                }
            }
        }
        return Util.JSONUtil.Load<T>(path);
    }

    /// <summary>
    /// プラットフォームのアプリパッケージからファイルを開く。
    /// デフォルト実装は null を返す。
    /// </summary>
    /// <param name="path">パッケージ内の相対パス</param>
    /// <returns>ストリーム、または null</returns>
    private partial Stream? OpenPackageFile(string path);

    public void SaveConfig<T>(string key, T data)
    {
        Util.JSONUtil.Save($"Configs/{key}.json", data);
    }

    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "MiniMikuDance_Temp");

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
    // Default implementation for non-MAUI platforms
    private partial Stream? OpenPackageFile(string path) => null;
}
