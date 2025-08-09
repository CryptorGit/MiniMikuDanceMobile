using System.Collections.Generic;

namespace MiniMikuDance.IK;

public class IkBoneMappingConfig
{
    public Dictionary<string, List<string>> Mapping { get; set; } = new();
}
