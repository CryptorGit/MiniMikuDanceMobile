using System;
using System.Collections.Generic;

[Serializable]
public class UIButtonConfig
{
    public string label;
    public string message;
}

[Serializable]
public class UIToggleConfig
{
    public string label;
    public string id;
    public bool defaultValue;
}

[Serializable]
public class UIConfig
{
    public List<UIButtonConfig> buttons = new List<UIButtonConfig>();
    public List<UIToggleConfig> toggles = new List<UIToggleConfig>();
    public bool showProgressBar;
    public bool showMessage;
}
