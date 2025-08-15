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
    Static,
    Dynamic,
    DynamicAndBonePosition
}

public enum JointType
{
    Spring6DOF,
    NoSpring6DOF,
    P2P,
    ConeTwist,
    Slider,
    Hinge
}

public class RigidBodyData
{
    public string Name { get; set; } = string.Empty;
    public int BoneIndex { get; set; } = -1;
    public byte Group { get; set; }
    public ushort GroupTarget { get; set; }
    public RigidBodyShape Shape { get; set; }
    public Vector3 Size { get; set; } = Vector3.Zero;
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public float Mass { get; set; }
    public float TranslationAttenuation { get; set; }
    public float RotationAttenuation { get; set; }
    public float Recoil { get; set; }
    public RigidBodyPhysicsType PhysicsType { get; set; }
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
    public JointType Type { get; set; }
    public int RigidBodyA { get; set; } = -1;
    public int RigidBodyB { get; set; } = -1;
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 TranslationMinLimit { get; set; } = Vector3.Zero;
    public Vector3 TranslationMaxLimit { get; set; } = Vector3.Zero;
    public Vector3 RotationMinLimit { get; set; } = Vector3.Zero;
    public Vector3 RotationMaxLimit { get; set; } = Vector3.Zero;
    public Vector3 TranslationSpring { get; set; } = Vector3.Zero;
    public Vector3 RotationSpring { get; set; } = Vector3.Zero;
    public Vector3 SpringTranslation { get; set; } = Vector3.Zero;
    public Vector3 SpringRotation { get; set; } = Vector3.Zero;
    public Vector3 LinearLowerLimit { get; set; } = Vector3.Zero;
    public Vector3 LinearUpperLimit { get; set; } = Vector3.Zero;
    public Vector3 AngularLowerLimit { get; set; } = Vector3.Zero;
    public Vector3 AngularUpperLimit { get; set; } = Vector3.Zero;
}
