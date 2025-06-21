using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays generated motions on a humanoid model by updating bone transforms
/// directly each frame. An Animation component can also be used for clips.
/// </summary>
public class MotionPlayer : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float playbackSpeed = 1f;

    private MotionData _motion;
    private float _time;
    private bool _playing;

    private readonly Dictionary<string, Transform> _boneMap = new();

    public void LoadMotion(MotionData data)
    {
        _motion = data;
        _time = 0f;
        CacheBones();
    }

    public void Play()
    {
        if (_motion == null)
        {
            Debug.LogWarning("MotionPlayer.Play called with no motion loaded");
            return;
        }
        _playing = true;
    }

    public void Pause() => _playing = false;

    public void Stop()
    {
        _playing = false;
        _time = 0f;
    }

    private void Update()
    {
        if (!_playing || _motion == null) return;

        _time += Time.deltaTime * playbackSpeed;
        int frame = Mathf.FloorToInt(_time / _motion.frameInterval);
        if (frame >= _motion.boneCurves["Hips"].positions.Length)
        {
            Stop();
            return;
        }

        foreach (var kvp in _motion.boneCurves)
        {
            if (!_boneMap.TryGetValue(kvp.Key, out var bone)) continue;
            bone.localPosition = kvp.Value.positions[frame];
            bone.localRotation = kvp.Value.rotations[frame];
        }
    }

    private void CacheBones()
    {
        _boneMap.Clear();
        if (animator == null) return;
        foreach (var boneName in _motion.boneCurves.Keys)
        {
            if (Enum.TryParse<HumanBodyBones>(boneName, out var hb))
            {
                var t = animator.GetBoneTransform(hb);
                if (t != null)
                {
                    _boneMap[boneName] = t;
                }
            }
        }
    }
}
