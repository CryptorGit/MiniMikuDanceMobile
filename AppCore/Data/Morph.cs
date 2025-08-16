using System.Collections.Generic;
using System.Numerics;
using MMDTools;
using MiniMikuDance.Import;

namespace MiniMikuDance.Data;

public class Morph
{
    public int Index { get; set; }
    public string NameJa { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public MorphType Type { get; set; }
    public MorphCategory Category { get; set; }
    public IList<MorphObject> Objects { get; set; } = new List<MorphObject>();
}

public abstract class MorphObject
{
    public int Index { get; set; }
}

public sealed class VertexMorphObject : MorphObject
{
    public Vector3 Offset { get; set; }
}

public sealed class GroupMorphObject : MorphObject
{
    public int MorphIndex { get; set; }
    public float Rate { get; set; }
}

public sealed class BoneMorphObject : MorphObject
{
    public Vector3 Translation { get; set; }
    public Quaternion Rotation { get; set; }
}

public sealed class UvMorphObject : MorphObject
{
    public Vector4 Offset { get; set; }
}

public sealed class MaterialMorphObject : MorphObject
{
    public bool IsAll { get; set; }
    public MaterialCalcMode CalcMode { get; set; }
    public Vector4 Diffuse { get; set; }
    public Vector3 Specular { get; set; }
    public float SpecularPower { get; set; }
    public Vector4 EdgeColor { get; set; }
    public float EdgeSize { get; set; }
    public Vector3 ToonColor { get; set; }
    public Vector4 TextureTint { get; set; }
}
