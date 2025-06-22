using System.IO;

namespace MiniMikuDance.Data;

public class DataManager : Util.Singleton<DataManager>
{
    public T LoadConfig<T>(string key) where T : new()
    {
        return Util.JSONUtil.Load<T>($"Configs/{key}.json");
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
