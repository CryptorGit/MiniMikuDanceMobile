using System;
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

    private float _time;

    public void UpdateFromDevice(float dt)
    {
        _time += dt;
        var pos = new Vector3(MathF.Sin(_time) * 0.1f, 0, 0);
        var rot = Quaternion.Identity;
        UpdatePose(pos, rot);
    }

    public void UpdatePose(System.Numerics.Vector3 position, System.Numerics.Quaternion rotation)
    {
        CurrentPose = new Pose { Position = position, Rotation = rotation };
    }
}
