using System;
using System.Numerics;
using System.Collections.Generic;

namespace MiniMikuDance.Import;

public class BoneData
{
    public string Name { get; set; } = string.Empty;
    public int Parent { get; set; } = -1;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 Translation { get; set; } = Vector3.Zero;
    public Matrix4x4 BindMatrix { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 InverseBindMatrix { get; set; } = Matrix4x4.Identity;
    public Vector3 BaseForward { get; set; } = Vector3.UnitZ;
    public Vector3 BaseUp { get; set; } = Vector3.UnitY;
    public Vector3 BaseRight { get; set; } = Vector3.UnitX;
    public List<int> Children { get; } = new();
    public IkInfo? Ik { get; set; }
}

public class IkInfo
{
    public int Target { get; set; } = -1;
    public List<IkLinkInfo> Chain { get; } = new();
    public Dictionary<string, int> Effectors { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public class IkLinkInfo
{
    public int Bone { get; set; }
    public int Parent { get; set; }
    public Vector3 BaseForward { get; set; }
    public Vector3 BaseUp { get; set; }
    public Vector3? LimitMin { get; set; }
    public Vector3? LimitMax { get; set; }
}
