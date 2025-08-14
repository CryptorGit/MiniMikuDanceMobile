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
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Size { get; set; }
    public float LinearDamping { get; set; }
    public float AngularDamping { get; set; }
    public float Restitution { get; set; }
    public float Friction { get; set; }
    public int Group { get; set; }
    public int Mask { get; set; }
    public int TransformType { get; set; }
}

public class JointData
{
    public string Name { get; set; } = string.Empty;
    public int RigidBodyA { get; set; } = -1;
    public int RigidBodyB { get; set; } = -1;
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 LinearLowerLimit { get; set; }
    public Vector3 LinearUpperLimit { get; set; }
    public Vector3 AngularLowerLimit { get; set; }
    public Vector3 AngularUpperLimit { get; set; }
    public Vector3 LinearStiffness { get; set; }
    public Vector3 AngularStiffness { get; set; }
}

