namespace MiniMikuDance.Import;

using System.Collections.Generic;
using MMDTools;

public class MorphData
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public MorphType Type { get; set; }
    public MorphCategory Category { get; set; }
    public List<MorphOffset> Offsets { get; set; } = new();
}

public struct MorphOffset
{
    public int Index { get; set; }
    public System.Numerics.Vector3 Vertex;
    public GroupOffset Group;
    public BoneOffset Bone;
    public UvOffset Uv;
    public UvOffset Uv1;
    public UvOffset Uv2;
    public UvOffset Uv3;
    public UvOffset Uv4;
    public FlipOffset Flip;
    public ImpulseOffset Impulse;
    public MaterialOffset Material;
}

public struct GroupOffset
{
    public int MorphIndex { get; set; }
    public float Rate { get; set; }
}

public struct BoneOffset
{
    public System.Numerics.Vector3 Translation { get; set; }
    public System.Numerics.Quaternion Rotation { get; set; }
}

public struct UvOffset
{
    public System.Numerics.Vector4 Offset { get; set; }
}

public struct FlipOffset
{
    public int MorphIndex { get; set; }
    public float Rate { get; set; }
}

public struct ImpulseOffset
{
    public int RigidBodyIndex { get; set; }
    public bool IsLocal { get; set; }
    public System.Numerics.Vector3 Velocity { get; set; }
    public System.Numerics.Vector3 Torque { get; set; }
}

public enum MaterialCalcMode
{
    Mul = 0,
    Add = 1
}

public enum MorphCategory
{
    SystemReserved = 0,
    Eyebrow = 1,
    Eye = 2,
    Mouth = 3,
    Other = 4
}

public struct MaterialOffset
{
    public bool IsAll { get; set; }
    public MaterialCalcMode CalcMode { get; set; }
    public System.Numerics.Vector4 Diffuse { get; set; }
    public System.Numerics.Vector3 Specular { get; set; }
    public float SpecularPower { get; set; }
    public System.Numerics.Vector4 EdgeColor { get; set; }
    public float EdgeSize { get; set; }
    public System.Numerics.Vector3 ToonColor { get; set; }
    public System.Numerics.Vector4 TextureTint { get; set; }
}
