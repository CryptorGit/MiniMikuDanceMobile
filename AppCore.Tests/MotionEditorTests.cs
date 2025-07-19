using System.Collections.Generic;
using MiniMikuDance.Motion;
using MiniMikuDance.PoseEstimation;
using System.Numerics;
using Xunit;

public class MotionEditorTests
{
    private MotionEditor CreateEditor()
    {
        var md = new MotionData
        {
            FrameInterval = 0.1f,
            Frames = new [] { new JointData(), new JointData() }
        };
        md.KeyFrames["bone"] = new SortedSet<int>();
        return new MotionEditor(md);
    }

    [Fact]
    public void AddRemove_UndoRedo()
    {
        var editor = CreateEditor();
        editor.AddKeyFrame("bone", 1);
        Assert.True(editor.HasKeyFrame("bone",1));
        editor.Undo();
        Assert.False(editor.HasKeyFrame("bone",1));
        editor.Redo();
        Assert.True(editor.HasKeyFrame("bone",1));
        editor.RemoveKeyFrame("bone",1);
        Assert.False(editor.HasKeyFrame("bone",1));
    }

    [Fact]
    public void CopyPasteRange_Works()
    {
        var editor = CreateEditor();
        editor.AddKeyFrame("b",1);
        editor.AddKeyFrame("b",3);
        editor.CopyRange(new[]{"b"},1,3);
        editor.RemoveRange(new[]{"b"},0,5);
        Assert.False(editor.HasKeyFrame("b",1));
        editor.PasteRange(5);
        Assert.True(editor.HasKeyFrame("b",5));
        Assert.True(editor.HasKeyFrame("b",7));
    }

    [Fact]
    public void GetNeighborKeyFrames_ReturnsCorrect()
    {
        var editor = CreateEditor();
        editor.AddKeyFrame("b",1);
        editor.AddKeyFrame("b",5);
        var (prev,next) = editor.GetNeighborKeyFrames("b",3);
        Assert.Equal(1, prev);
        Assert.Equal(5, next);
    }
}
