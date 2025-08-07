namespace MiniMikuDance.Import;

using System.Collections.Generic;
using MMDTools;

public class MorphData
{
    public string Name { get; set; } = string.Empty;
    public MorphType Type { get; set; }
    public List<MorphOffset> Offsets { get; set; } = new();
}

public struct MorphOffset
{
    public int Index { get; set; }
    public System.Numerics.Vector3 Offset { get; set; }
}
