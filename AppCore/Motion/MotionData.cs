namespace MiniMikuDance.Motion;

using MiniMikuDance.PoseEstimation;

public class MotionData
{
    public float FrameInterval { get; set; }
    public JointData[] Frames { get; set; } = System.Array.Empty<JointData>();
}
