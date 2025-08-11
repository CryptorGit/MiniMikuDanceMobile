using System.Numerics;

namespace MiniMikuDance.IK;

public class IkBone
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Quaternion BaseRotation { get; }
    public Vector3 BasePosition { get; }
    public Vector3 BaseForward { get; }
    public Vector3 BaseUp { get; }
    public int PmxBoneIndex { get; }
    public bool IsSelected { get; set; }

    public IkBone(int pmxBoneIndex, Vector3 position, Quaternion baseRotation, Vector3 baseForward, Vector3 baseUp)
    {
        PmxBoneIndex = pmxBoneIndex;
        Position = position;
        BasePosition = position;
        BaseRotation = baseRotation;
        BaseForward = baseForward;
        BaseUp = baseUp;
        Rotation = baseRotation;
    }
}
