using System.Numerics;
using MiniMikuDance.Motion;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;
using Xunit;

namespace AppCore.Tests;

public class MotionApplierTests
{
    [Fact]
    public void ApplyUpdatesBoneTranslation()
    {
        var model = new ModelData();
        model.Bones.Add(new BoneData { Name = "root", Parent = -1 });
        model.Bones.Add(new BoneData { Name = "child", Parent = 0 });
        var applier = new MotionApplier(model);

        var joint = new JointData
        {
            Positions = new Vector3[]
            {
                new Vector3(0,0,0),
                new Vector3(0,1,0)
            }
        };

        applier.Apply(joint);

        Assert.Equal(new Vector3(0,0,0), model.Bones[0].Translation);
        Assert.Equal(new Vector3(0,1,0), model.Bones[1].Translation);
        Assert.Equal(Quaternion.Identity, model.Bones[1].Rotation);
    }

    [Fact]
    public void ApplyCalculatesBoneRotation()
    {
        var model = new ModelData();
        model.Bones.Add(new BoneData { Name = "root", Parent = -1 });
        model.Bones.Add(new BoneData { Name = "child", Parent = 0 });
        var applier = new MotionApplier(model);

        var joint = new JointData
        {
            Positions = new Vector3[]
            {
                Vector3.Zero,
                new Vector3(1,0,0)
            }
        };

        applier.Apply(joint);

        var expected = Quaternion.CreateFromAxisAngle(new Vector3(0,0,-1), MathF.PI/2);
        Assert.Equal(expected.X, model.Bones[1].Rotation.X, 3);
        Assert.Equal(expected.Y, model.Bones[1].Rotation.Y, 3);
        Assert.Equal(expected.Z, model.Bones[1].Rotation.Z, 3);
        Assert.Equal(expected.W, model.Bones[1].Rotation.W, 3);
    }
}
