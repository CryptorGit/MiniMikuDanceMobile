using MiniMikuDance.Util;

namespace MiniMikuDance.UI;

public class UIManager : Singleton<UIManager>
{
    public UIConfig Config { get; private set; } = new();

    public void LoadConfig(string path)
    {
        Config = JSONUtil.Load<UIConfig>(path);
    }

    public void SaveConfig(string path)
    {
        JSONUtil.Save(path, Config);
    }
}
