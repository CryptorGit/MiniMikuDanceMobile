using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.IK;

public class IkBone
{
    public int ConstraintIndex { get; }
    public string Name { get; }
    public Vector3 Position { get; set; }
    public int PmxBoneIndex { get; }
    public bool IsSelected { get; set; }
    public IkConstraintData Constraint { get; }

    public IkBone(int pmxBoneIndex, int constraintIndex, string name, Vector3 position, IkConstraintData constraint)
    {
        PmxBoneIndex = pmxBoneIndex;
        ConstraintIndex = constraintIndex;
        Name = name;
        Position = position;
        Constraint = constraint;
    }
}

