namespace MiniMikuDance.UI;

public class UIConfig
{
    public List<UIButton> Buttons { get; set; } = new();
    public List<UIToggle> Toggles { get; set; } = new();
    public bool ShowProgressBar { get; set; }
    public bool ShowMessage { get; set; }
    public bool ShowRecordingIndicator { get; set; }
    public bool ShowThumbnail { get; set; }
}
