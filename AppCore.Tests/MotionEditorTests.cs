using MiniMikuDance.Motion;
using MiniMikuDance.PoseEstimation;
using System.Numerics;

namespace AppCore.Tests;

public class MotionEditorTests
{
    private static MotionData CreateMotion(int frameCount = 3, int jointCount = 1)
    {
        var frames = new JointData[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            frames[i] = new JointData
            {
                Timestamp = i * 0.1f,
                Positions = new Vector3[jointCount],
                Confidences = new float[jointCount]
            };
        }
        return new MotionData
        {
            FrameInterval = 0.1f,
            Frames = frames
        };
    }

    [Fact]
    public void AddKeyFrame_AddsAndSavesHistory()
    {
        var motion = CreateMotion();
        var editor = new MotionEditor(motion);

        editor.AddKeyFrame("bone", 1);

        Assert.True(motion.KeyFrames.TryGetValue("bone", out var set) && set.Contains(1));
        Assert.True(editor.CanUndo);
    }

    [Fact]
    public void RemoveKeyFrame_RemovesAndSavesHistory()
    {
        var motion = CreateMotion();
        var editor = new MotionEditor(motion);

        editor.AddKeyFrame("bone", 1);
        editor.RemoveKeyFrame("bone", 1);

        Assert.False(editor.HasKeyFrame("bone", 1));
        Assert.True(editor.CanUndo);
    }

    [Fact]
    public void UndoRedo_WorksForAddRemove()
    {
        var motion = CreateMotion();
        var editor = new MotionEditor(motion);

        editor.AddKeyFrame("bone", 0);
        editor.AddKeyFrame("bone", 1);
        editor.RemoveKeyFrame("bone", 0);

        Assert.Equal(new[] { 1 }, motion.KeyFrames["bone"].ToArray());

        Assert.True(editor.Undo());
        Assert.Equal(new[] { 0, 1 }, motion.KeyFrames["bone"].ToArray());

        Assert.True(editor.Undo());
        Assert.Equal(new[] { 0 }, motion.KeyFrames["bone"].ToArray());

        Assert.True(editor.Redo());
        Assert.Equal(new[] { 0, 1 }, motion.KeyFrames["bone"].ToArray());

        Assert.True(editor.Redo());
        Assert.Equal(new[] { 1 }, motion.KeyFrames["bone"].ToArray());
    }
}
