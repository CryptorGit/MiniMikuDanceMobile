namespace MiniMikuDance.Import;

using System.Collections.Generic;
using MMDTools;

public class MorphData
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public MorphType Type { get; set; }
    public List<MorphOffset> Offsets { get; set; } = new();
}

public struct MorphOffset
{
    public int Index { get; set; }
    public System.Numerics.Vector3 Vertex;
    public GroupOffset Group;
    public BoneOffset Bone;
    public UvOffset Uv;
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

public enum MaterialCalcMode
{
    Mul = 0,
    Add = 1
}

public struct MaterialOffset
{
    public bool IsAll { get; set; }
    public MaterialCalcMode CalcMode { get; set; }
    public System.Numerics.Vector4 Diffuse { get; set; }
}
