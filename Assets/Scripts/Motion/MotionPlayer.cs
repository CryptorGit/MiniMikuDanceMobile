using UnityEngine;

/// <summary>
/// Simple runtime player for MotionData.
/// Applies joint positions to assigned transforms each frame.
/// </summary>
public class MotionPlayer : MonoBehaviour
{
    [SerializeField] private Transform[] jointTargets;

    private MotionData _motion;
    private float _time;
    private bool _playing;

    public void LoadMotion(MotionData data)
    {
        _motion = data;
        _time = 0f;
    }

    public void Play()
    {
        if (_motion == null) return;
        _playing = true;
    }

    public void Pause()
    {
        _playing = false;
    }

    public void Stop()
    {
        _playing = false;
        _time = 0f;
    }

    private void Update()
    {
        if (!_playing || _motion == null || _motion.boneCurves.Count == 0)
            return;

        int frameIndex = Mathf.FloorToInt(_time / _motion.frameInterval);
        if (frameIndex >= _motion.boneCurves["Joint0"].positions.Length)
        {
            Stop();
            return;
        }

        int jointCount = Mathf.Min(jointTargets.Length, _motion.boneCurves.Count);
        for (int i = 0; i < jointCount; i++)
        {
            if (!jointTargets[i]) continue;
            if (_motion.boneCurves.TryGetValue($"Joint{i}", out var curve))
            {
                jointTargets[i].localPosition = curve.positions[frameIndex];
                if (curve.rotations != null && curve.rotations.Length > frameIndex)
                {
                    jointTargets[i].localRotation = curve.rotations[frameIndex];
                }
            }
        }

        _time += Time.deltaTime;
    }
}
