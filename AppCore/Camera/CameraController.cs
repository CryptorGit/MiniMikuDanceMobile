using System.Numerics;

namespace MiniMikuDance.Camera;

public class CameraController
{
    private bool _gyroEnabled;
    private readonly ARPoseManager _arPoseManager = new();
    private System.Numerics.Vector3 _position;
    private System.Numerics.Quaternion _rotation = System.Numerics.Quaternion.Identity;
    private System.Numerics.Quaternion _gyroRotation = System.Numerics.Quaternion.Identity;

    public System.Numerics.Vector3 Position => _position;
    public System.Numerics.Quaternion Rotation => _rotation;

    public void EnableGyro(bool on) => _gyroEnabled = on;

    public void SetGyroRotation(System.Numerics.Quaternion rotation)
    {
        _gyroRotation = rotation;
    }

    public void SyncGyro()
    {
        if (!_gyroEnabled) return;
        _rotation = _gyroRotation;
    }

    public void Update()
    {
        SyncARPose();
        SyncGyro();
    }

    public void SyncARPose()
    {
        // Update the camera transform from the latest AR pose information.
        var pose = _arPoseManager.CurrentPose;
        _position = pose.Position;
        _rotation = pose.Rotation;
    }
}
