using System.Numerics;

namespace MiniMikuDance.Camera;

public struct Pose
{
    public Vector3 Position;
    public Quaternion Rotation;
}

public class ARPoseManager
{
    public Pose CurrentPose { get; set; }
}
