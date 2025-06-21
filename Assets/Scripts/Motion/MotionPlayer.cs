using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple runtime motion player that applies MotionData directly to bones.
/// </summary>
public class MotionPlayer : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private bool _playing;
    private float _time;
    private MotionData _motion;
    private Dictionary<string, Transform> _boneMap;

    /// <summary>
    /// Load motion data and bone mapping for playback.
    /// </summary>
    public void Load(MotionData data, Dictionary<string, Transform> boneMap)
    {
        _motion = data;
        _boneMap = boneMap;
        _time = 0f;
    }

    public void Play()
    {
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
        if (!_playing || _motion == null)
            return;

        _time += Time.deltaTime;
        int frame = Mathf.FloorToInt(_time / _motion.frameInterval);
        if (frame >= FrameCount())
        {
            _playing = false;
            return;
        }
        ApplyFrame(frame);
    }

    private int FrameCount()
    {
        foreach (var curve in _motion.boneCurves.Values)
        {
            return curve.rotations.Length;
        }
        return 0;
    }

    private void ApplyFrame(int frame)
    {
        foreach (var kv in _motion.boneCurves)
        {
            if (!_boneMap.TryGetValue(kv.Key, out var t))
                continue;
            var curve = kv.Value;
            if (curve.rotations != null && frame < curve.rotations.Length)
                t.localRotation = curve.rotations[frame];
            if (curve.positions != null && frame < curve.positions.Length)
                t.localPosition = curve.positions[frame];
        }
    }
}
