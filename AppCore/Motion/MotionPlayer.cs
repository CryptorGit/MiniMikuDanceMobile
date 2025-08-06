using System;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionPlayer
{
    private MotionData? _current;
    private int _frameIndex;
    private bool _playing;
    private float _elapsed;

    public event Action<JointData>? OnFramePlayed;

    public void Play(MotionData data)
    {
        _current = data;
        _frameIndex = 0;
        _elapsed = 0f;
        _playing = true;
    }

    public void Update(float deltaTime)
    {
        if (!_playing || _current == null || _current.Frames.Length == 0)
            return;

        _elapsed += deltaTime;
        while (_elapsed >= _current.FrameInterval && _playing)
        {
            _elapsed -= _current.FrameInterval;
            if (_frameIndex < _current.Frames.Length)
            {
                OnFramePlayed?.Invoke(_current.Frames[_frameIndex]);
                _frameIndex++;
            }
            else
            {
                _playing = false;
            }
        }
    }
}
