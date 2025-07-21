using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MiniMikuDance.App;

public class BoneLimit
{
    public string Bone { get; set; } = string.Empty;
    public Vector3 Min { get; set; }
    public Vector3 Max { get; set; }
}

public class BonesConfig
{
    public List<BoneLimit> HumanoidBoneLimits { get; set; } = new();

    public bool TryGetLimit(string bone, out BoneLimit? limit)
    {
        limit = HumanoidBoneLimits.FirstOrDefault(l => string.Equals(l.Bone, bone, StringComparison.OrdinalIgnoreCase));
        return limit != null;
    }
}
