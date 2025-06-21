using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Converts pose estimation results into animation data.
/// This simple implementation records joint positions and
/// exposes an AnimationClip for the root transform.
/// </summary>
public class MotionGenerator : MonoBehaviour
{
    /// <summary>
    /// Generate MotionData from an array of JointData.
    /// Each landmark is stored as a separate bone curve.
    /// </summary>
    public MotionData GenerateData(JointData[] joints)
    {
        var data = new MotionData();
        if (joints == null || joints.Length == 0)
            return data;

        int jointCount = joints[0].positions.Length;
        for (int i = 0; i < jointCount; i++)
        {
            data.boneCurves[$"Joint{i}"] = new BoneCurve
            {
                positions = new Vector3[joints.Length],
                rotations = new Quaternion[joints.Length]
            };
        }

        for (int frame = 0; frame < joints.Length; frame++)
        {
            for (int j = 0; j < jointCount; j++)
            {
                data.boneCurves[$"Joint{j}"].positions[frame] = joints[frame].positions[j];
                data.boneCurves[$"Joint{j}"].rotations[frame] = Quaternion.identity;
            }
        }

        if (joints.Length > 1)
            data.frameInterval = joints[1].timestamp - joints[0].timestamp;

        return data;
    }

    /// <summary>
    /// Apply simple moving average smoothing to the motion data.
    /// </summary>
    public void Smooth(MotionData data, int window)
    {
        if (data == null || window < 2)
            return;

        foreach (var curve in data.boneCurves.Values)
        {
            var src = curve.positions;
            var dst = new Vector3[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;
                for (int j = Mathf.Max(0, i - window); j <= Mathf.Min(src.Length - 1, i + window); j++)
                {
                    sum += src[j];
                    count++;
                }
                dst[i] = sum / count;
            }
            curve.positions = dst;
        }
    }

    /// <summary>
    /// Create a very simple AnimationClip using the root joint position.
    /// </summary>
    public AnimationClip GenerateClip(MotionData data)
    {
        var clip = new AnimationClip();
        if (data == null || data.boneCurves.Count == 0)
            return clip;

        clip.frameRate = 1f / data.frameInterval;
        if (!data.boneCurves.TryGetValue("Joint0", out var root))
            return clip;

        var curveX = new AnimationCurve();
        var curveY = new AnimationCurve();
        var curveZ = new AnimationCurve();
        for (int i = 0; i < root.positions.Length; i++)
        {
            float t = i * data.frameInterval;
            Vector3 p = root.positions[i];
            curveX.AddKey(t, p.x);
            curveY.AddKey(t, p.y);
            curveZ.AddKey(t, p.z);
        }

        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x", curveX);
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.y", curveY);
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.z", curveZ);
        return clip;
    }
}
