using System.Numerics;
using MiniMikuDance.Motion;
using MiniMikuDance.PoseEstimation;

namespace AppCore.Tests;

public class MotionGeneratorTests
{
    [Fact]
    public void Generate_ComputesIntervalAndFrames()
    {
        var joints = new[]
        {
            new JointData { Timestamp = 0f },
            new JointData { Timestamp = 0.5f },
            new JointData { Timestamp = 1f }
        };
        var generator = new MotionGenerator();

        var motion = generator.Generate(joints);

        Assert.Equal(0.5f, motion.FrameInterval, precision: 3);
        Assert.Equal(joints.Length, motion.Frames.Length);
        for (int i = 0; i < joints.Length; i++)
        {
            Assert.Equal(joints[i].Timestamp, motion.Frames[i].Timestamp, precision: 3);
        }
    }

    [Fact]
    public void Generate_WithNoJoints_ReturnsDefaultInterval()
    {
        var generator = new MotionGenerator();

        var motion = generator.Generate(Array.Empty<JointData>());

        Assert.Equal(1f / 30f, motion.FrameInterval, precision: 3);
        Assert.Empty(motion.Frames);
    }
}
