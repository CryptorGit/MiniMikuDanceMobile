using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionData
{
    public float FrameInterval { get; set; }
    public JointData[] Frames { get; set; } = Array.Empty<JointData>();
}

public class MotionGenerator
{
    public MotionData Generate(JointData[] joints)
    {
        if (joints.Length == 0)
            return new MotionData { FrameInterval = 1f / 30f };

        float interval = joints.Length > 1
            ? (joints[^1].Timestamp - joints[0].Timestamp) / (joints.Length - 1)
            : 1f / 30f;

        return new MotionData
        {
            FrameInterval = interval,
            Frames = joints
        };
    }
}
