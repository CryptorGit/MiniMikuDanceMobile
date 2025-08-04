using System.Numerics;

namespace MiniMikuDance.Camera;

public class CameraController
{
    private readonly ARPoseManager _arPoseManager = new();
    private System.Numerics.Vector3 _position;
    private System.Numerics.Quaternion _rotation = System.Numerics.Quaternion.Identity;

    public System.Numerics.Vector3 Position => _position;
    public System.Numerics.Quaternion Rotation => _rotation;

    public void Update()
    {
        SyncARPose();
    }

    public void SyncARPose()
    {
        // Update the camera transform from the latest AR pose information.
        var pose = _arPoseManager.CurrentPose;
        _position = pose.Position;
        _rotation = pose.Rotation;
    }
}
