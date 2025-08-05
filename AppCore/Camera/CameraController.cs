using System.Numerics;

namespace MiniMikuDance.Camera;

public class CameraController
{
    private readonly ARPoseManager _arPoseManager = new();
    private Vector3 _position;
    private Quaternion _rotation = Quaternion.Identity;

    public Vector3 Position => _position;
    public Quaternion Rotation => _rotation;

    public void Update()
    {
        SyncARPose();
    }

    public void SyncARPose()
    {
        var pose = _arPoseManager.CurrentPose;
        _position = pose.Position;
        _rotation = pose.Rotation;
    }
}
