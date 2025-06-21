namespace MiniMikuDance.Camera;

public class CameraController
{
    private bool _gyroEnabled;
    public System.Numerics.Vector3 Position { get; private set; }
    public System.Numerics.Quaternion Rotation { get; private set; } = System.Numerics.Quaternion.Identity;

    public void EnableGyro(bool on) => _gyroEnabled = on;

    public void SyncGyro()
    {
        if (!_gyroEnabled) return;
        // Gyro input is platform dependent; simulate by small rotation
        Rotation *= System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, 0.001f);
    }

    public void SyncARPose(Pose pose)
    {
        Position = pose.Position;
        Rotation = pose.Rotation;
    }
}
