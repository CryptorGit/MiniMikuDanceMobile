using System;
using System.Numerics;

namespace MiniMikuDance.Camera;

public struct Pose
{
    public Vector3 Position;
    public Quaternion Rotation;
}

public partial class ARPoseManager
{
    public Pose CurrentPose { get; private set; }

    private float _time;

    public void UpdateFromDevice(float dt)
    {
#if ANDROID || IOS
        if (TryGetPlatformPose(out var pos, out var rot))
        {
            UpdatePose(pos, rot);
            return;
        }
#endif

        // Fallback for environments without AR support
        _time += dt;
        var dummyPos = new Vector3(MathF.Sin(_time) * 0.1f, 0, 0);
        var dummyRot = Quaternion.Identity;
        UpdatePose(dummyPos, dummyRot);
    }

    public void UpdatePose(System.Numerics.Vector3 position, System.Numerics.Quaternion rotation)
    {
        CurrentPose = new Pose { Position = position, Rotation = rotation };
    }

#if ANDROID || IOS
    /// <summary>
    /// Try to obtain pose information from the underlying AR framework.
    /// Platform specific implementations are provided per target.
    /// </summary>
    /// <param name="position">World position.</param>
    /// <param name="rotation">World rotation.</param>
    /// <returns>True if a pose could be retrieved.</returns>
    private partial bool TryGetPlatformPose(out Vector3 position, out Quaternion rotation);
#endif
}
