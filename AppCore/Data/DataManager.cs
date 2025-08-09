using System.IO;

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

    public void SaveConfig<T>(string key, T data)
    {
        Util.JSONUtil.Save($"Configs/{key}.json", data);
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
