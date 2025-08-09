using System.Numerics;
using MiniMikuDance.IK;
using Xunit;

namespace AppCore.Tests;

public class IkManagerTests
{
    public IkManagerTests()
    {
        IkManager.Clear();
        IkManager.PickFunc = (x, y) => 0;
        IkManager.GetBonePositionFunc = _ => new Vector3(1f, 2f, 3f);
        IkManager.GetCameraPositionFunc = () => new Vector3(1f, 2f, 5f);
        IkManager.ToModelSpaceFunc = v => new Vector3(v.X, v.Y, -v.Z);
    }

    [Fact]
    public void PickBone_GeneratesPlaneInModelSpace()
    {
        var idx = IkManager.PickBone(0f, 0f);
        Assert.Equal(0, idx);
        var plane = IkManager.DragPlane;
        Assert.Equal(new Vector3(0f, 0f, -1f), plane.Normal);
        Assert.Equal(-3f, plane.D, 5);
    }

    [Fact]
    public void IntersectDragPlane_UsesModelSpace()
    {
        IkManager.PickBone(0f, 0f);
        var rayOrigin = new Vector3(1f, 2f, 5f);
        var rayDir = new Vector3(0f, 0f, -1f);
        var pos = IkManager.IntersectDragPlane((rayOrigin, rayDir));
        Assert.True(pos.HasValue);
        Assert.Equal(new Vector3(1f, 2f, -3f), pos.Value);
    }
}
