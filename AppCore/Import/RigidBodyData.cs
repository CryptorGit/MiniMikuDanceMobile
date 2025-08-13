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
    public System.Numerics.Vector3 Size { get; set; }
    public float LinearDamping { get; set; }
    public float AngularDamping { get; set; }
}

public class JointData
{
    public string Name { get; set; } = string.Empty;
    public int RigidBodyA { get; set; } = -1;
    public int RigidBodyB { get; set; } = -1;
    public System.Numerics.Vector3 LinearLowerLimit { get; set; }
    public System.Numerics.Vector3 LinearUpperLimit { get; set; }
    public System.Numerics.Vector3 AngularLowerLimit { get; set; }
    public System.Numerics.Vector3 AngularUpperLimit { get; set; }
    public SpringData LinearSpring { get; set; }
    public SpringData AngularSpring { get; set; }
}

public struct SpringData
{
    public float Frequency;
    public float DampingRatio;
}

