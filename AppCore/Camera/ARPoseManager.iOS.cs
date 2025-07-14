#if IOS
using ARKit;
using System.Numerics;

namespace MiniMikuDance.Camera;

public partial class ARPoseManager
{
    private readonly ARSession _session = new ARSession();
    private bool _sessionStarted;

    private void EnsureSession()
    {
        if (_sessionStarted)
            return;
        var config = new ARWorldTrackingConfiguration();
        _session.Run(config);
        _sessionStarted = true;
    }

    private partial bool TryGetPlatformPose(out Vector3 position, out Quaternion rotation)
    {
        EnsureSession();
        var frame = _session.CurrentFrame;
        if (frame == null)
        {
            position = default;
            rotation = Quaternion.Identity;
            return false;
        }

        var t = frame.Camera.Transform;
        position = new Vector3(t.Column3.X, t.Column3.Y, t.Column3.Z);
        var matrix = new Matrix4x4(
            t.Column0.X, t.Column0.Y, t.Column0.Z, t.Column0.W,
            t.Column1.X, t.Column1.Y, t.Column1.Z, t.Column1.W,
            t.Column2.X, t.Column2.Y, t.Column2.Z, t.Column2.W,
            t.Column3.X, t.Column3.Y, t.Column3.Z, t.Column3.W);
        rotation = Quaternion.CreateFromRotationMatrix(matrix);
        return true;
    }
}
#endif
