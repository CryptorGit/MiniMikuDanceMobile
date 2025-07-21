using System.Collections.Generic;
using System.IO;
using OpenTK.Mathematics;
using MiniMikuDance.Util;

namespace MiniMikuDance.Import;

public class RotationLimit
{
    public string Bone { get; set; } = string.Empty;
    public System.Numerics.Vector3 Min { get; set; }
    public System.Numerics.Vector3 Max { get; set; }
}

public class RotationLimitData
{
    public List<RotationLimit> HumanoidBoneLimits { get; set; } = new();
}

public static class RotationLimitLoader
{
    public static Dictionary<string, (Vector3 Min, Vector3 Max)> Load(string path)
    {
        using var stream = File.OpenRead(path);
        var data = JSONUtil.LoadFromStream<RotationLimitData>(stream);
        var map = new Dictionary<string, (Vector3 Min, Vector3 Max)>();
        foreach (var limit in data.HumanoidBoneLimits)
        {
            map[limit.Bone] = (limit.Min.ToOpenTK(), limit.Max.ToOpenTK());
        }
        return map;
    }
}
