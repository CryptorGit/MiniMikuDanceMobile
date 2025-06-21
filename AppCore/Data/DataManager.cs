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

    public void CleanupTemp() { }
}
