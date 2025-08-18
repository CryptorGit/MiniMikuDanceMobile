using System.Numerics;

namespace MiniMikuDance.Import;

public enum RigidBodyShape
{
    Sphere,
    Box,
    Capsule
}

public enum RigidBodyMode
{
    Static,
    Dynamic,
    DynamicAndBonePosition
}

public class RigidBodyData
{
    public string Name { get; set; } = string.Empty;
    public int BoneIndex { get; set; } = -1;
    public float Mass { get; set; }
    public RigidBodyShape Shape { get; set; }
    public byte Group { get; set; }
    public ushort GroupMask { get; set; }
    public float TranslationDamping { get; set; }
    public float RotationDamping { get; set; }
    public float Restitution { get; set; }
    public float Friction { get; set; }
    public RigidBodyMode Mode { get; set; }
}

public class JointData
{
    public string Name { get; set; } = string.Empty;
    public int RigidBodyA { get; set; } = -1;
    public int RigidBodyB { get; set; } = -1;
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 TranslationLimitMin { get; set; } = Vector3.Zero;
    public Vector3 TranslationLimitMax { get; set; } = Vector3.Zero;
    public Vector3 RotationLimitMin { get; set; } = Vector3.Zero;
    public Vector3 RotationLimitMax { get; set; } = Vector3.Zero;
    public Vector3 TranslationSpring { get; set; } = Vector3.Zero;
    public Vector3 RotationSpring { get; set; } = Vector3.Zero;
}

