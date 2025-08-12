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
    public IkInfo? Ik { get; set; }
}

public class IkInfo
{
    public int Target { get; set; } = -1;
    public int Iterations { get; set; } = 0;
    public float RotationLimit { get; set; } = 0f;
    public List<IkLink> Links { get; } = new();
}

public class IkLink
{
    public int BoneIndex { get; set; } = -1;
    public bool HasLimit { get; set; }
    public Vector3 MinAngle { get; set; } = Vector3.Zero;
    public Vector3 MaxAngle { get; set; } = Vector3.Zero;
}
