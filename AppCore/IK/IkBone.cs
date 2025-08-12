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
    public Vector3 DefaultPlaneNormal { get; }
    public int PmxBoneIndex { get; }
    public bool IsSelected { get; set; }
    public Vector3 PoleVector { get; set; } = Vector3.Zero;
    public float RotationLimit { get; set; }

    public IkBone(int pmxBoneIndex, Vector3 position, Quaternion baseRotation, Quaternion rotation, Vector3 baseForward, Vector3 baseUp, Vector3 defaultPlaneNormal)
    {
        PmxBoneIndex = pmxBoneIndex;
        Position = position;
        BasePosition = position;
        BaseRotation = baseRotation;
        Rotation = rotation;
        BaseForward = baseForward;
        BaseUp = baseUp;
        DefaultPlaneNormal = defaultPlaneNormal;
    }
}
