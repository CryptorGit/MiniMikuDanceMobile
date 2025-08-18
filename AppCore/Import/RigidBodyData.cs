using System.Numerics;

namespace MiniMikuDance.Import;

public enum RigidBodyShape
{
    Sphere,
    Box,
    Capsule
}

public class RigidBodyData
{
    public string Name { get; set; } = string.Empty;
    public int BoneIndex { get; set; } = -1;
    public float Mass { get; set; }
    public RigidBodyShape Shape { get; set; }
    public float LinearDamping { get; set; }
    public float AngularDamping { get; set; }
    public float Restitution { get; set; }
    public float Friction { get; set; }
    public byte Group { get; set; }
    public ushort Mask { get; set; }
    public int Mode { get; set; }
}

public class JointData
{
    public string Name { get; set; } = string.Empty;
    public int RigidBodyA { get; set; } = -1;
    public int RigidBodyB { get; set; } = -1;
    public Vector3 PositionMin { get; set; } = Vector3.Zero;
    public Vector3 PositionMax { get; set; } = Vector3.Zero;
    public Vector3 RotationMin { get; set; } = Vector3.Zero;
    public Vector3 RotationMax { get; set; } = Vector3.Zero;
    public Vector3 SpringPosition { get; set; } = Vector3.Zero;
    public Vector3 SpringRotation { get; set; } = Vector3.Zero;
}

