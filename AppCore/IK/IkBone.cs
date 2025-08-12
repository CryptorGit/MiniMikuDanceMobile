using System.Numerics;

namespace MiniMikuDance.IK;

public class IkBone
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Quaternion BaseRotation { get; }
    public Quaternion BaseRotationInv { get; }
    public Vector3 BasePosition { get; }
    public Vector3 BaseForward { get; }
    public Vector3 BaseUp { get; }
    public Vector3 BasePlaneNormal { get; }
    public Vector3 PrevUp { get; set; }
    public int PmxBoneIndex { get; }
    public bool IsSelected { get; set; }
    public Vector3 PoleVector { get; set; } = Vector3.Zero;
    public float RotationLimit { get; set; }

    public IkBone(int pmxBoneIndex, Vector3 position, Quaternion baseRotation, Vector3 baseForward, Vector3 baseUp)
    {
        PmxBoneIndex = pmxBoneIndex;
        Position = position;
        BasePosition = position;
        BaseRotation = baseRotation;
        BaseRotationInv = Quaternion.Inverse(baseRotation);
        Rotation = baseRotation;
        BaseForward = baseForward;
        BaseUp = baseUp;
        BasePlaneNormal = Vector3.Normalize(Vector3.Cross(baseForward, baseUp));
        PrevUp = baseUp;
    }
}
