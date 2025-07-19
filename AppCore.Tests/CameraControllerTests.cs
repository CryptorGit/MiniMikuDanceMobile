using System.Numerics;
using System.Reflection;
using MiniMikuDance.Camera;
using Xunit;

public class CameraControllerTests
{
    private static ARPoseManager GetManager(CameraController c)
    {
        var field = typeof(CameraController).GetField("_arPoseManager", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (ARPoseManager)field.GetValue(c)!;
    }

    [Fact]
    public void SyncARPose_UpdatesPositionAndRotation()
    {
        var cam = new CameraController();
        var mgr = GetManager(cam);
        var pos = new Vector3(1,2,3);
        var rot = Quaternion.CreateFromYawPitchRoll(0.1f,0.2f,0.3f);
        mgr.UpdatePose(pos, rot);
        cam.SyncARPose();
        Assert.Equal(pos, cam.Position);
        Assert.Equal(rot, cam.Rotation);
    }

    [Fact]
    public void Update_WithGyroEnabled_PrefersGyroRotation()
    {
        var cam = new CameraController();
        var mgr = GetManager(cam);
        mgr.UpdatePose(Vector3.Zero, Quaternion.Identity);
        var gyro = Quaternion.CreateFromYawPitchRoll(0,1,0);
        cam.SetGyroRotation(gyro);
        cam.EnableGyro(true);
        cam.Update();
        Assert.Equal(gyro, cam.Rotation);
    }
}
