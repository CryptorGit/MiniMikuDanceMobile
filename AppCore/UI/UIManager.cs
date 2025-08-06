using MiniMikuDance.Util;

namespace MiniMikuDance.UI;

public class UIManager : Singleton<UIManager>
{
    public float Progress { get; set; }
    public float ExtractProgress { get; set; }
    public float PoseProgress { get; set; }
    public string Message { get; private set; } = string.Empty;
    public bool IsRecording { get; set; }

    public void SetMessage(string message)
    {
        Message = message;
    }
}
