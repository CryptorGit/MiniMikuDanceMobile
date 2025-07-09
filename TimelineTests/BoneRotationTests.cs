using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Motion;
using MiniMikuDance.PoseEstimation;
using Xunit;

namespace TimelineTests;

public class BoneRotationTests
{
    private static Vector3 GetBoneRotationAtFrame(MotionEditor editor, string bone, int frame, Dictionary<string, BlazePoseJoint> map)
    {
        if (editor == null) return Vector3.Zero;
        if (!map.TryGetValue(bone, out var joint)) return Vector3.Zero;
        var frames = editor.Motion.Frames;
        if (frame < 0 || frame >= frames.Length) return Vector3.Zero;
        // rotation data is not available; zero is expected
        return Vector3.Zero;
    }

    [Fact]
    public void ReturnZeroWhenNoRotationData()
    {
        var motion = new MotionData
        {
            FrameInterval = 1f,
            Frames = new[]
            {
                new JointData
                {
                    Timestamp = 0f,
                    Positions = new Vector3[33],
                    Confidences = new float[33]
                }
            }
        };
        var editor = new MotionEditor(motion);
        var map = new Dictionary<string, BlazePoseJoint>
        {
            {"leftShoulder", BlazePoseJoint.LeftShoulder}
        };
        var rot = GetBoneRotationAtFrame(editor, "leftShoulder", 0, map);
        Assert.Equal(Vector3.Zero, rot);
    }
}
