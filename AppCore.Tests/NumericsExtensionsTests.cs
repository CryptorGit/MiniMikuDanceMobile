using System.Numerics;
using OpenTK.Mathematics;
using MiniMikuDance.Util;
using Xunit;

namespace AppCore.Tests;

public class NumericsExtensionsTests
{
    [Fact]
    public void ToMatrix4_Identity_ReturnsIdentity()
    {
        var mat = Matrix4x4.Identity;
        var result = mat.ToMatrix4();
        Assert.Equal(Matrix4.Identity, result);
    }

    [Fact]
    public void FromEulerDegrees_Zero_ReturnsIdentityQuaternion()
    {
        var quat = System.Numerics.Vector3.Zero.FromEulerDegrees();
        Assert.Equal(System.Numerics.Quaternion.Identity, quat);
    }
}
