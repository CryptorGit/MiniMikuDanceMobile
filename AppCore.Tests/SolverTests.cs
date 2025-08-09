using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using MiniMikuDance.IK;
using MiniMikuDance.Import;
using Xunit;

namespace AppCore.Tests;

public class SolverTests
{
    private static ModelData LoadModel()
    {
        using var importer = new ModelImporter();
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../StreamingAssets/Models/fFm31ndHAO/艾莲.pmx"));
        return importer.ImportModel(path);
    }

    [Fact]
    public void TwoBoneSolver_ArmChainFromPmx_CalculatesRotationAndLengths()
    {
        var data = LoadModel();
        var bones = data.Bones;

        int shoulderIdx = FindBoneIndex(bones, "Bip001 L UpperArm");
        int elbowIdx = FindBoneIndex(bones, "Bip001 L Forearm");
        int handIdx = FindBoneIndex(bones, "Bip001 L Hand");

        var shoulderPos = Vector3.Transform(Vector3.Zero, bones[shoulderIdx].BindMatrix);
        var elbowPos = Vector3.Transform(Vector3.Zero, bones[elbowIdx].BindMatrix);
        var handPos = Vector3.Transform(Vector3.Zero, bones[handIdx].BindMatrix);

        var shoulder = new IkBone(shoulderIdx, shoulderPos, Quaternion.Identity);
        var elbow = new IkBone(elbowIdx, elbowPos, Quaternion.Identity);
        var hand = new IkBone(handIdx, handPos, Quaternion.Identity);

        var chain = new[] { shoulder, elbow, hand };
        var solver = new TwoBoneSolver(Vector3.Distance(shoulderPos, elbowPos),
                                       Vector3.Distance(elbowPos, handPos));

        var target = handPos + new Vector3(0.05f, 0.02f, 0f);
        hand.Position = target;
        solver.Solve(chain);

        Assert.Equal(Vector3.Distance(shoulderPos, elbowPos),
            Vector3.Distance(shoulder.Position, elbow.Position), 4);
        Assert.Equal(Vector3.Distance(elbowPos, handPos),
            Vector3.Distance(elbow.Position, hand.Position), 4);

        var rootForward = Vector3.Normalize(elbow.Position - shoulder.Position);
        var rotatedRoot = Vector3.Transform(Vector3.UnitZ, shoulder.Rotation);
        Assert.True(Vector3.Distance(rootForward, rotatedRoot) < 1e-4f);

        var midForward = Vector3.Normalize(hand.Position - elbow.Position);
        var rotatedMid = Vector3.Transform(Vector3.UnitZ, elbow.Rotation);
        Assert.True(Vector3.Distance(midForward, rotatedMid) < 1e-4f);
    }

    [Fact]
    public void FabrikSolver_LegChainFromPmx_CalculatesRotationAndLengths()
    {
        var data = LoadModel();
        var bones = data.Bones;

        int hipIdx = FindBoneIndex(bones, "Bip001 L Thigh");
        int kneeIdx = FindBoneIndex(bones, "Bip001 L Calf");
        int footIdx = FindBoneIndex(bones, "Bip001 L Foot");

        var hipPos = Vector3.Transform(Vector3.Zero, bones[hipIdx].BindMatrix);
        var kneePos = Vector3.Transform(Vector3.Zero, bones[kneeIdx].BindMatrix);
        var footPos = Vector3.Transform(Vector3.Zero, bones[footIdx].BindMatrix);

        var hip = new IkBone(hipIdx, hipPos, Quaternion.Identity);
        var knee = new IkBone(kneeIdx, kneePos, Quaternion.Identity);
        var foot = new IkBone(footIdx, footPos, Quaternion.Identity);

        var chain = new[] { hip, knee, foot };
        var lengths = new[]
        {
            Vector3.Distance(hipPos, kneePos),
            Vector3.Distance(kneePos, footPos)
        };
        var solver = new FabrikSolver(lengths);

        var target = footPos + new Vector3(0.02f, -0.01f, 0.03f);
        foot.Position = target;
        solver.Solve(chain);

        Assert.Equal(lengths[0], Vector3.Distance(hip.Position, knee.Position), 4);
        Assert.Equal(lengths[1], Vector3.Distance(knee.Position, foot.Position), 4);

        var rootForward = Vector3.Normalize(knee.Position - hip.Position);
        var rotatedRoot = Vector3.Transform(Vector3.UnitZ, hip.Rotation);
        Assert.True(Vector3.Distance(rootForward, rotatedRoot) < 1e-4f);

        var midForward = Vector3.Normalize(foot.Position - knee.Position);
        var rotatedMid = Vector3.Transform(Vector3.UnitZ, knee.Rotation);
        Assert.True(Vector3.Distance(midForward, rotatedMid) < 1e-4f);
    }

    private static int FindBoneIndex(IReadOnlyList<BoneData> bones, string name)
    {
        for (int i = 0; i < bones.Count; i++)
        {
            if (string.Equals(bones[i].Name, name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        throw new KeyNotFoundException(name);
    }
}

