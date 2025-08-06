using System.Numerics;

namespace MiniMikuDance.Camera;

public class CameraController
{
    private Vector3 _position = Vector3.Zero;
    private Quaternion _rotation = Quaternion.Identity;

    public Vector3 Position => _position;
    public Quaternion Rotation => _rotation;

    public void Update()
    {
    }
}
