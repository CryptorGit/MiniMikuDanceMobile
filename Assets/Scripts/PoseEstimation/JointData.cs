using System;
using UnityEngine;

[Serializable]
public struct JointData
{
    /// <summary>
    /// Timestamp in seconds from the start of the video frame.
    /// </summary>
    public float timestamp;

    /// <summary>
    /// 3D positions of pose landmarks. Expected size is 33.
    /// </summary>
    public Vector3[] positions;

    /// <summary>
    /// Confidence for each landmark.
    /// </summary>
    public float[] confidences;
}
