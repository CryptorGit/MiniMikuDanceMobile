using System.Numerics;
using System.Collections.Generic;

namespace MiniMikuDance.Import;

public class BoneData
{
    public string Name { get; set; } = string.Empty;
    public int Parent { get; set; } = -1;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 Translation { get; set; } = Vector3.Zero;
    public Vector3 BaseForward { get; set; } = Vector3.UnitZ;
    public Vector3 BaseUp { get; set; } = Vector3.UnitY;
    public Matrix4x4 BindMatrix { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 InverseBindMatrix { get; set; } = Matrix4x4.Identity;
    public IkInfo? Ik { get; set; }
}

public class IkInfo
{
    public int Target { get; set; } = -1;
    public List<int> Chain { get; } = new();
}
