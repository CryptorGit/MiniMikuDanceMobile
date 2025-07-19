using SVector3 = System.Numerics.Vector3;
using SVector4 = System.Numerics.Vector4;
using SQuaternion = System.Numerics.Quaternion;
using OVector3 = OpenTK.Mathematics.Vector3;
using OQuaternion = OpenTK.Mathematics.Quaternion;
using OpenTK.Mathematics;
using MiniMikuDance.Util;
using Xunit;

public class NumericsExtensionsMoreTests
{
    [Fact]
    public void ToVector4_Conversion()
    {
        var v = new SVector4(1,2,3,4);
        var result = v.ToVector4();
        Assert.Equal(1, result.X);
        Assert.Equal(2, result.Y);
        Assert.Equal(3, result.Z);
        Assert.Equal(4, result.W);
    }

    [Fact]
    public void ToMatrix4_FromQuaternion()
    {
        var q = SQuaternion.CreateFromYawPitchRoll(MathF.PI/2, 0, 0);
        var mat = q.ToMatrix4();
        var expected = Matrix4.CreateFromQuaternion(new OQuaternion(q.X, q.Y, q.Z, q.W));
        Assert.Equal(expected, mat);
    }

    [Fact]
    public void OpenTkRoundTrip()
    {
        var v = new SVector3(1,2,3);
        var otk = v.ToOpenTK();
        var back = otk.ToNumerics();
        Assert.Equal(v, back);
    }

    [Fact]
    public void ToEulerDegrees_RoundTrip()
    {
        var quat = new SQuaternion(0,0,0,1);
        var euler = quat.ToEulerDegrees();
        var back = euler.FromEulerDegrees();
        Assert.True(SQuaternion.Dot(quat, back) > 0.999f);
    }
}
