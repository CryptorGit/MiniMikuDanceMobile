using System;
using UnityEngine;

/// <summary>
/// Represents the pose estimation result for a single frame.
/// Positions array should contain 33 landmark positions in world space.
/// </summary>
[Serializable]
public struct JointData
{
    public float timestamp;
    public Vector3[] positions;
    public float[] confidences;
}
