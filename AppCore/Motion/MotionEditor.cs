using System;
using System.Collections.Generic;
using System.Linq;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionEditor
{
    private readonly Dictionary<string, SortedSet<int>> _keyFrames = new();
    private Dictionary<string, List<int>> _clipboard = new();

    private class EditorState
    {
        public JointData[] Frames { get; set; } = Array.Empty<JointData>();
        public Dictionary<string, SortedSet<int>> KeyFrames { get; set; } = new();
    }

    private const int HistoryLimit = 20;
    private readonly List<EditorState> _history = new();
    private int _historyIndex = -1;

    public MotionData Motion { get; }

    public MotionEditor(MotionData motion)
    {
        Motion = motion;
        foreach (var kv in motion.KeyFrames)
        {
            _keyFrames[kv.Key] = new SortedSet<int>(kv.Value);
        }
        SaveState();
    }

    private static JointData CloneJoint(JointData src)
        => new()
        {
            Timestamp = src.Timestamp,
            Positions = src.Positions.ToArray(),
            Confidences = src.Confidences.ToArray()
        };

    private EditorState CaptureState()
    {
        return new EditorState
        {
            Frames = Motion.Frames.Select(CloneJoint).ToArray(),
            KeyFrames = _keyFrames.ToDictionary(kv => kv.Key, kv => new SortedSet<int>(kv.Value))
        };
    }

    public void SaveState()
    {
        if (_historyIndex >= 0 && _historyIndex < _history.Count - 1)
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
        _history.Add(CaptureState());
        if (_history.Count > HistoryLimit)
            _history.RemoveAt(0);
        _historyIndex = _history.Count - 1;
    }

    private void ApplyState(EditorState state)
    {
        Motion.Frames = state.Frames.Select(CloneJoint).ToArray();
        _keyFrames.Clear();
        Motion.KeyFrames.Clear();
        foreach (var kv in state.KeyFrames)
        {
            _keyFrames[kv.Key] = new SortedSet<int>(kv.Value);
            Motion.KeyFrames[kv.Key] = new SortedSet<int>(kv.Value);
        }
    }

    public bool CanUndo => _historyIndex > 0;
    public bool CanRedo => _historyIndex < _history.Count - 1;

    public bool Undo()
    {
        if (!CanUndo) return false;
        _historyIndex--;
        ApplyState(_history[_historyIndex]);
        return true;
    }

    public bool Redo()
    {
        if (!CanRedo) return false;
        _historyIndex++;
        ApplyState(_history[_historyIndex]);
        return true;
    }

    public void AddKeyFrame(string bone, int frame, bool recordHistory = true)
    {
        if (!_keyFrames.TryGetValue(bone, out var set))
        {
            set = new SortedSet<int>();
            _keyFrames[bone] = set;
        }
        bool changed = set.Add(frame);
        if (!Motion.KeyFrames.TryGetValue(bone, out var mset))
        {
            mset = new SortedSet<int>();
            Motion.KeyFrames[bone] = mset;
        }
        changed |= mset.Add(frame);
        if (changed && recordHistory)
            SaveState();
    }

    public void RemoveKeyFrame(string bone, int frame, bool recordHistory = true)
    {
        bool changed = false;
        if (_keyFrames.TryGetValue(bone, out var set))
            changed |= set.Remove(frame);
        if (Motion.KeyFrames.TryGetValue(bone, out var mset))
            changed |= mset.Remove(frame);
        if (changed && recordHistory)
            SaveState();
    }

    public bool HasKeyFrame(string bone, int frame)
    {
        return (_keyFrames.TryGetValue(bone, out var set) && set.Contains(frame))
            || (Motion.KeyFrames.TryGetValue(bone, out var mset) && mset.Contains(frame));
    }

    public (int? Prev, int? Next) GetNeighborKeyFrames(string bone, int frame)
    {
        if (!_keyFrames.TryGetValue(bone, out var set))
            return (null, null);

        int? prev = set.Where(f => f < frame).DefaultIfEmpty().Max();
        int? next = set.Where(f => f > frame).DefaultIfEmpty().Min();
        return (prev == 0 && !set.Contains(0) ? null : prev, next == 0 && !set.Contains(0) ? null : next);
    }

    public Dictionary<string, List<int>> GetKeyFramesInRange(IEnumerable<string> bones, int startFrame, int endFrame)
    {
        var result = new Dictionary<string, List<int>>();
        if (startFrame > endFrame)
            (startFrame, endFrame) = (endFrame, startFrame);
        foreach (var bone in bones)
        {
            if (_keyFrames.TryGetValue(bone, out var set))
            {
                var list = set.GetViewBetween(startFrame, endFrame).ToList();
                if (list.Count > 0)
                    result[bone] = list;
            }
        }
        return result;
    }

    public void CopyRange(IEnumerable<string> bones, int startFrame, int endFrame)
    {
        var tmp = GetKeyFramesInRange(bones, startFrame, endFrame);
        _clipboard = new Dictionary<string, List<int>>();
        foreach (var kv in tmp)
            _clipboard[kv.Key] = kv.Value.Select(f => f - startFrame).ToList();
    }

    public void PasteRange(int startFrame)
    {
        foreach (var kv in _clipboard)
        {
            foreach (var off in kv.Value)
                AddKeyFrame(kv.Key, startFrame + off, false);
        }
        if (_clipboard.Count > 0)
            SaveState();
    }

    public void RemoveRange(IEnumerable<string> bones, int startFrame, int endFrame)
    {
        if (startFrame > endFrame)
            (startFrame, endFrame) = (endFrame, startFrame);
        foreach (var bone in bones)
        {
            if (_keyFrames.TryGetValue(bone, out var set))
            {
                var toRemove = set.GetViewBetween(startFrame, endFrame).ToList();
                foreach (var f in toRemove)
                    RemoveKeyFrame(bone, f, false);
            }
        }
        SaveState();
    }

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

