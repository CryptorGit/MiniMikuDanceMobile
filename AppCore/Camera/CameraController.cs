namespace MiniMikuDance.Camera;

public class CameraController
{
    private bool _gyroEnabled;

    public void EnableGyro(bool on) => _gyroEnabled = on;
    public void SyncGyro() { /* stub */ }
    public void SyncARPose() { /* stub */ }
}
