namespace MiniMikuDance.Motion;

public class MotionPlayer
{
    public float PlaybackSpeed { get; private set; } = 1f;

    public void Play(MotionData data) { /* stub */ }
    public void Pause() { }
    public void Stop() { }
    public void Restart() { }
    public void SetPlaybackSpeed(float speed) => PlaybackSpeed = speed;
}
