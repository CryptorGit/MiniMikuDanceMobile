using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable container for generated motion data.
/// </summary>
[Serializable]
public class MotionData
{
    public float frameInterval = 1f / 30f;
    public Dictionary<string, BoneCurve> boneCurves = new Dictionary<string, BoneCurve>();
}

[Serializable]
public class BoneCurve
{
    public Quaternion[] rotations;
    public Vector3[] positions;
}
