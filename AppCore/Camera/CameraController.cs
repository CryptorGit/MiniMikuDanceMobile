using System.Numerics;

namespace MiniMikuDance.Camera;

public class CameraController
{
    private bool _gyroEnabled;
    private readonly ARPoseManager _arPoseManager = new();
    private System.Numerics.Vector3 _position;
    private System.Numerics.Quaternion _rotation = System.Numerics.Quaternion.Identity;

    public System.Numerics.Vector3 Position => _position;
    public System.Numerics.Quaternion Rotation => _rotation;

    public void EnableGyro(bool on) => _gyroEnabled = on;

    public void SyncGyro()
    {
        if (!_gyroEnabled) return;
        // Gyroscope access is platform dependent; here we simply rotate
        // the camera slightly each call to simulate gyro motion.
        var delta = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, 0.01f);
        _rotation = System.Numerics.Quaternion.Normalize(System.Numerics.Quaternion.Concatenate(_rotation, delta));
    }

    public void SyncARPose()
    {
        // Update the camera transform from the latest AR pose information.
        var pose = _arPoseManager.CurrentPose;
        _position = pose.Position;
        _rotation = pose.Rotation;
    }
}
