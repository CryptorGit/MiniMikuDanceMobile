#if ANDROID
using System.Numerics;
using Android.Content;
using Google.AR.Core;

namespace MiniMikuDance.Camera;

public partial class ARPoseManager
{
    private readonly Session _session;

    public ARPoseManager()
    {
        var context = Android.App.Application.Context;
        _session = new Session(context);
        var config = new Config(_session);
        _session.Configure(config);
        _session.Resume();
    }

    private partial bool TryGetPlatformPose(out Vector3 position, out Quaternion rotation)
    {
        var frame = _session.Update();
        var cam = frame.Camera;
        var pose = cam.DisplayOrientedPose;
        var t = pose.Translation;
        position = new Vector3(t[0], t[1], t[2]);
        var r = pose.RotationQuaternion;
        rotation = new Quaternion(r[0], r[1], r[2], r[3]);
        return true;
    }
}
#endif
