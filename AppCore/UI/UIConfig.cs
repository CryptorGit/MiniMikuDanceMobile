namespace MiniMikuDance.UI;

public class UIButton
{
    public string Label { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class UIToggle
{
    public string Label { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public bool DefaultValue { get; set; }
}

public class UIConfig
{
    public List<UIButton> Buttons { get; set; } = new();
    public List<UIToggle> Toggles { get; set; } = new();
    public bool ShowMessage { get; set; }
    public bool ShowThumbnail { get; set; }
}
