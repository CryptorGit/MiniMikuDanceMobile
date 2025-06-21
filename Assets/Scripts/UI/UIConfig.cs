using System;
using System.Collections.Generic;

[Serializable]
public class UIButtonConfig
{
    public string label;
    public string message;
}

[Serializable]
public class UIConfig
{
    public List<UIButtonConfig> buttons = new List<UIButtonConfig>();
}
