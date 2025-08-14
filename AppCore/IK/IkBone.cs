using System.Numerics;

namespace MiniMikuDance.IK;

public enum BoneRole
{
    None,
    Ankle,
    Knee
}

public class IkBone
{
    public string Name { get; }
    public BoneRole Role { get; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Quaternion BaseRotation { get; }
    public Vector3 BasePosition { get; }
    public Vector3 BaseForward { get; }
    public Vector3 BaseUp { get; }
    public Vector3 BasePlaneNormal { get; }
    public Vector3 PrevUp { get; set; }
    public int PmxBoneIndex { get; }
    public bool IsSelected { get; set; }

    public IkBone(int pmxBoneIndex, string name, BoneRole role, Vector3 position, Quaternion baseRotation, Vector3 baseForward, Vector3 baseUp)
    {
        PmxBoneIndex = pmxBoneIndex;
        Name = name;
        Role = role;
        Position = position;
        BasePosition = position;
        BaseRotation = baseRotation;
        Rotation = baseRotation;
        BaseForward = baseForward;
        BaseUp = baseUp;
        BasePlaneNormal = Vector3.Normalize(Vector3.Cross(baseForward, baseUp));
        PrevUp = baseUp;
    }
}
