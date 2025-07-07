using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionEditor
{
    private readonly Dictionary<string, SortedSet<int>> _keyFrames = new();

    public MotionData Motion { get; }

    public MotionEditor(MotionData motion)
    {
        Motion = motion;
    }

    public void AddKeyFrame(string bone, int frame)
    {
        if (!_keyFrames.TryGetValue(bone, out var set))
        {
            set = new SortedSet<int>();
            _keyFrames[bone] = set;
        }
        set.Add(frame);
        EnsureFrameCount(frame + 1);
    }

    public void RemoveKeyFrame(string bone, int frame)
    {
        if (_keyFrames.TryGetValue(bone, out var set))
            set.Remove(frame);
    }

    public bool HasKeyFrame(string bone, int frame)
        => _keyFrames.TryGetValue(bone, out var set) && set.Contains(frame);

    private void EnsureFrameCount(int count)
    {
        if (Motion.Frames.Length >= count)
            return;

        var list = Motion.Frames.ToList();
        var last = Motion.Frames.Length > 0 ? Motion.Frames[^1] : new JointData
        {
            Timestamp = 0f,
            Positions = new System.Numerics.Vector3[0],
            Confidences = new float[0]
        };

        while (list.Count < count)
            list.Add(last);

        Motion.Frames = list.ToArray();
    }
}

