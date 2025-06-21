using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionData
{
    public float FrameInterval { get; set; }
}

public class MotionGenerator
{
    public MotionData Generate(JointData[] joints)
    {
        // Placeholder: convert joints to motion data
        return new MotionData { FrameInterval = 1f / 30f };
    }
}
