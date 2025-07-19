using System.Numerics;
using MiniMikuDance.Camera;
using Xunit;

public class ARPoseManagerTests
{
    [Fact]
    public void UpdatePose_SetsCurrentPose()
    {
        var mgr = new ARPoseManager();
        var pos = new Vector3(1, 2, 3);
        var rot = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        mgr.UpdatePose(pos, rot);
        Assert.Equal(pos, mgr.CurrentPose.Position);
        Assert.Equal(rot, mgr.CurrentPose.Rotation);
    }

    [Fact]
    public void UpdateFromDevice_AccumulatesTime()
    {
        var mgr = new ARPoseManager();
        mgr.UpdateFromDevice(1f);
        var expected1 = new Vector3(MathF.Sin(1f) * 0.1f, 0, 0);
        Assert.Equal(expected1, mgr.CurrentPose.Position);
        mgr.UpdateFromDevice(1f);
        var expected2 = new Vector3(MathF.Sin(2f) * 0.1f, 0, 0);
        Assert.Equal(expected2, mgr.CurrentPose.Position);
    }
}
