using MiniMikuDance.Motion;
using MiniMikuDance.Import;
using System.Numerics;
using System.Collections.Generic;
using Xunit;

public class IKSolverTests
{
    [Fact]
    public void SolveLeg_ReachesTarget()
    {
        var model = new ModelData();
        model.Bones.Add(new BoneData { Name = "leftUpperLeg", Parent = -1, Translation = Vector3.Zero, Rotation = Quaternion.Identity });
        model.Bones.Add(new BoneData { Name = "leftLowerLeg", Parent = 0, Translation = new Vector3(0, -1, 0), Rotation = Quaternion.Identity });
        model.Bones.Add(new BoneData { Name = "leftFoot", Parent = 1, Translation = new Vector3(0, -1, 0), Rotation = Quaternion.Identity });
        model.HumanoidBones["leftUpperLeg"] = 0;
        model.HumanoidBones["leftLowerLeg"] = 1;
        model.HumanoidBones["leftFoot"] = 2;

        var rotations = new List<Vector3> { Vector3.Zero, Vector3.Zero, Vector3.Zero };
        var translations = new List<Vector3> { Vector3.Zero, Vector3.Zero, Vector3.Zero };

        Vector3 target = new Vector3(0, -2, 1);
        Vector3 pole = new Vector3(0, 0, 1);

        Vector3 result = IKSolver.SolveLeg(model, rotations, translations, "leftUpperLeg", "leftLowerLeg", "leftFoot", target , pole);
        Vector3 initial = new Vector3(0, -2, 0);
        Assert.True(Vector3.Distance(result, initial) > 0.1f);

    }
}
