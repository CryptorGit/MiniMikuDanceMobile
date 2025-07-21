using System.IO;

namespace MiniMikuDance.Data;

public class DataManager : Util.Singleton<DataManager>
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
                File.Copy(src, path);
            }
        }
        return Util.JSONUtil.Load<T>(path);
    }

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
