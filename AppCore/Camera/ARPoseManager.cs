namespace MiniMikuDance.Camera;

public struct Pose
{
    public System.Numerics.Vector3 Position;
    public System.Numerics.Quaternion Rotation;
}

public class ARPoseManager
{
    public Pose CurrentPose { get; private set; }
}
