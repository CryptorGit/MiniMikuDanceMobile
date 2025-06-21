using System;
using UnityEngine;

/// <summary>
/// Represents pose estimation results for a single frame.
/// </summary>
[Serializable]
public struct JointData
{
    public float timestamp;
    public Vector3[] positions;
    public float[] confidences;
}
