using System.Numerics;

namespace MiniMikuDance.IK;

public class IkBone
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Quaternion BaseRotation { get; }
    public Vector3 BasePosition { get; }
    public int PmxBoneIndex { get; }
    public bool IsSelected { get; set; }
    public Vector3? LowerLimit { get; set; }
    public Vector3? UpperLimit { get; set; }

    public IkBone(int pmxBoneIndex, Vector3 position, Quaternion baseRotation)
    {
        PmxBoneIndex = pmxBoneIndex;
        Position = position;
        BasePosition = position;
        BaseRotation = baseRotation;
        Rotation = baseRotation;
    }
}
