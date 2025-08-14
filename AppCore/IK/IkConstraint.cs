using System.Numerics;

namespace MiniMikuDance.IK;

public class IkConstraint
{
    public int TargetBoneIndex { get; }
    public int EffectorBoneIndex { get; }
    public Vector3 TargetPosition { get; set; }

    public IkConstraint(int targetBoneIndex, int effectorBoneIndex, Vector3 targetPosition)
    {
        TargetBoneIndex = targetBoneIndex;
        EffectorBoneIndex = effectorBoneIndex;
        TargetPosition = targetPosition;
    }
}
