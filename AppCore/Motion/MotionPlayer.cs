using System;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionPlayer
{
    private MotionData? _current;
    private int _frameIndex;
    private bool _playing;
    private float _elapsed;

    public float PlaybackSpeed { get; private set; } = 1f;
    public bool IsPlaying => _playing;
    public int FrameIndex => _frameIndex;
    public event Action<JointData>? OnFramePlayed;
    public event Action? OnStopped;

    public void Play(MotionData data)
    {
        _current = data;
        _frameIndex = 0;
        _elapsed = 0f;
        _playing = true;
    }

    public void Pause() => _playing = false;

    public void Resume()
    {
        if (_current != null)
            _playing = true;
    }

    public void Seek(int index)
    {
        if (_current == null)
            return;
        _frameIndex = Math.Clamp(index, 0, _current.Frames.Length - 1);
        _elapsed = 0f;
    }

    public void Stop()
    {
        _playing = false;
        _frameIndex = 0;
        _elapsed = 0f;
        OnStopped?.Invoke();
    }

    public void Restart()
    {
        if (_current == null) return;
        _frameIndex = 0;
        _elapsed = 0f;
        _playing = true;
    }

    public void SetPlaybackSpeed(float speed) => PlaybackSpeed = speed;

    public void Update(float deltaTime)
    {
        if (!_playing || _current == null || _current.Frames.Length == 0)
            return;

        _elapsed += deltaTime * PlaybackSpeed;
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
                OnStopped?.Invoke();
            }
        }
    }
}
