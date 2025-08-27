namespace MiniMikuDance.Import;

using System.Collections.Generic;
using MMDTools;

public class DisplayFrameElement
{
    public DisplayFrameElementTarget TargetType { get; set; }
    public int TargetIndex { get; set; }
}

public class DisplayFrameData
{
    public string Name { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public DisplayFrameType Type { get; set; }
    public List<DisplayFrameElement> Elements { get; set; } = new();
}
