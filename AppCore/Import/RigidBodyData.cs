using System.Numerics;

namespace MiniMikuDance.Import;

public enum RigidBodyShape
{
    Sphere,
    Box,
    Capsule
}

public enum RigidBodyPhysicsType
{
    FollowBone = 0,
    Physics = 1,
    PhysicsWithBoneAlignment = 2
}

public class RigidBodyData
{
    public string Name { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public int BoneIndex { get; set; } = -1;
    public float Mass { get; set; }
    public RigidBodyShape Shape { get; set; }
    public float TranslationAttenuation { get; set; }
    public float RotationAttenuation { get; set; }
    public float Recoil { get; set; }
    public float Friction { get; set; }
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 Size { get; set; } = Vector3.Zero;
    public byte Group { get; set; }
    public ushort GroupTarget { get; set; }
    public RigidBodyPhysicsType PhysicsType { get; set; }
}

public class JointData
{
    public string Name { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public int RigidBodyA { get; set; } = -1;
    public int RigidBodyB { get; set; } = -1;
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 PositionMin { get; set; } = Vector3.Zero;
    public Vector3 PositionMax { get; set; } = Vector3.Zero;
    public Vector3 RotationMin { get; set; } = Vector3.Zero;
    public Vector3 RotationMax { get; set; } = Vector3.Zero;
    public Vector3 SpringPosition { get; set; } = Vector3.Zero;
    public Vector3 SpringRotation { get; set; } = Vector3.Zero;
    public Vector3 DampingPosition { get; set; } = Vector3.Zero;
    public Vector3 DampingRotation { get; set; } = Vector3.Zero;
}

