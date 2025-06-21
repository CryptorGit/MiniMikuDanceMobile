using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MotionData
{
    /// <summary>
    /// Interval between frames in seconds (1 / frame rate).
    /// </summary>
    public float frameInterval;

    /// <summary>
    /// Animation curves keyed by bone name.
    /// </summary>
    public Dictionary<string, BoneCurve> boneCurves = new Dictionary<string, BoneCurve>();
}

[Serializable]
public class BoneCurve
{
    /// <summary>
    /// Rotation of the bone per frame.
    /// </summary>
    public Quaternion[] rotations;

    /// <summary>
    /// Optional positional offsets.
    /// </summary>
    public Vector3[] positions;
}
