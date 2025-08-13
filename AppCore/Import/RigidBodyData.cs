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
    public Vector3 Size { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public float TranslationDamping { get; set; }
    public float RotationDamping { get; set; }
    public float Restitution { get; set; }
    public float Friction { get; set; }
    public byte CollisionGroup { get; set; }
    public ushort CollisionMask { get; set; }
}

public class JointData
{
    public string Name { get; set; } = string.Empty;
    public int RigidBodyA { get; set; } = -1;
    public int RigidBodyB { get; set; } = -1;
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 LinearLowerLimit { get; set; }
    public Vector3 LinearUpperLimit { get; set; }
    public Vector3 LinearSpring { get; set; }
    public Vector3 AngularLowerLimit { get; set; }
    public Vector3 AngularUpperLimit { get; set; }
    public Vector3 AngularSpring { get; set; }
}

