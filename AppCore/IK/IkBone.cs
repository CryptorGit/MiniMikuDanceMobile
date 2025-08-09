using System.Numerics;

namespace MiniMikuDance.IK;

public class IkBone
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public int PmxBoneIndex { get; }

    public IkBone(int pmxBoneIndex, Vector3 position, Quaternion rotation)
    {
        PmxBoneIndex = pmxBoneIndex;
        Position = position;
        Rotation = rotation;
    }
}
