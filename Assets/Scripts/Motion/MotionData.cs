using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable container for generated motion data.
/// Stores per-bone position and rotation arrays sampled at a
/// constant frame interval.
/// </summary>
[Serializable]
public class MotionData
{
    /// <summary>
    /// Seconds between each frame.
    /// </summary>
    public float frameInterval = 1f / 30f;

    /// <summary>
    /// Mapping of bone name to animation curves.
    /// </summary>
    public Dictionary<string, BoneCurve> boneCurves = new Dictionary<string, BoneCurve>();
}

[Serializable]
public class BoneCurve
{
    public Vector3[] positions;
    public Quaternion[] rotations;
}
