using System;
using System.Threading;
using System.Threading.Tasks;
using MiniMikuDance.PoseEstimation;
namespace MiniMikuDance.Motion;

public class MotionPlayer
{
    private CancellationTokenSource? _cts;
    private MotionData? _current;

    public float PlaybackSpeed { get; private set; } = 1f;
    public event Action<JointData>? OnFrame;

    public async void Play(MotionData data)
    {
        Stop();
        _current = data;
        _cts = new CancellationTokenSource();
        for (int i = 0; i < data.Frames.Length; i++)
        {
            if (_cts.IsCancellationRequested) break;
            OnFrame?.Invoke(data.Frames[i]);
            await Task.Delay(TimeSpan.FromSeconds(data.FrameInterval / PlaybackSpeed));
        }
    }

    public void Pause() => _cts?.Cancel();

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    public void Restart()
    {
        if (_current != null)
            Play(_current);
    }

    public void SetPlaybackSpeed(float speed) => PlaybackSpeed = speed;
}
