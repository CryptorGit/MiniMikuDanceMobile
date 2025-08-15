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
    public Vector3 Size { get; set; } = Vector3.One;
    public float LinearDamping { get; set; }
    public float AngularDamping { get; set; }
    public float Restitution { get; set; }
    public float Friction { get; set; }
    public int CollisionGroup { get; set; }
    public int CollisionMask { get; set; } = -1;
}

public class JointData
{
    public string Name { get; set; } = string.Empty;
    public int RigidBodyA { get; set; } = -1;
    public int RigidBodyB { get; set; } = -1;
    public Vector3 SpringTranslation { get; set; } = Vector3.Zero;
    public Vector3 SpringRotation { get; set; } = Vector3.Zero;
    public Vector3 LinearLowerLimit { get; set; } = Vector3.Zero;
    public Vector3 LinearUpperLimit { get; set; } = Vector3.Zero;
    public Vector3 AngularLowerLimit { get; set; } = Vector3.Zero;
    public Vector3 AngularUpperLimit { get; set; } = Vector3.Zero;
}

