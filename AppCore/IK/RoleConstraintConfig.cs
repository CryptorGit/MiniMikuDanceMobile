using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Data;

namespace MiniMikuDance.IK;

public class Vec3
{
    public float? X { get; set; }
    public float? Y { get; set; }
    public float? Z { get; set; }

    public Vector3 ToVector3(float defaultValue)
        => new(X ?? defaultValue, Y ?? defaultValue, Z ?? defaultValue);
}

public class RoleConstraint
{
    public Vec3? RemoveAxis { get; set; }
    public Vec3? Min { get; set; }
    public Vec3? Max { get; set; }
}

public class RoleConstraintsConfig
{
    public Dictionary<string, RoleConstraint> Constraints { get; set; } = new();
}

internal static class RoleConstraintTable
{
    private static Dictionary<BoneRole, RoleConstraint>? _table;

    private static Dictionary<BoneRole, RoleConstraint> Load()
    {
        var cfg = DataManager.Instance.LoadConfig<RoleConstraintsConfig>("RoleConstraints");
        var dict = new Dictionary<BoneRole, RoleConstraint>();
        foreach (var kv in cfg.Constraints)
        {
            if (Enum.TryParse<BoneRole>(kv.Key, true, out var role))
                dict[role] = kv.Value;
        }
        return dict;
    }

    public static RoleConstraint? Get(BoneRole role)
    {
        _table ??= Load();
        return _table.TryGetValue(role, out var c) ? c : null;
    }
}
