using System.Numerics;

namespace MiniMikuDance.Camera;

public struct Pose
{
    public Vector3 Position;
    public Quaternion Rotation;
}

public class ARPoseManager
{
    public Pose CurrentPose { get; private set; }

    public void UpdatePose(System.Numerics.Vector3 position, System.Numerics.Quaternion rotation)
    {
        CurrentPose = new Pose { Position = position, Rotation = rotation };
    }
}
