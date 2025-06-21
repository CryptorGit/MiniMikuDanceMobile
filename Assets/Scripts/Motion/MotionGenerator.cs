using System;
using UnityEngine;

/// <summary>
/// Converts pose estimation results into animation data.
/// This is a simplified placeholder implementation.
/// </summary>
public class MotionGenerator
{
    /// <summary>
    /// Convert joint arrays to a serializable motion container.
    /// </summary>
    public MotionData GenerateData(JointData[] joints)
    {
        var data = new MotionData();
        int frameCount = joints.Length;
        foreach (var jointName in new[] {"Hips"})
        {
            var curve = new BoneCurve
            {
                positions = new Vector3[frameCount],
                rotations = new Quaternion[frameCount]
            };
            for (int i = 0; i < frameCount; i++)
            {
                curve.positions[i] = joints[i].positions[0];
                curve.rotations[i] = Quaternion.identity;
            }
            data.boneCurves[jointName] = curve;
        }
        return data;
    }

    /// <summary>
    /// Generate an AnimationClip from motion data.
    /// </summary>
    public AnimationClip GenerateClip(MotionData data)
    {
        var clip = new AnimationClip { frameRate = 1f / data.frameInterval };
        foreach (var kvp in data.boneCurves)
        {
            var bone = kvp.Key;
            var curve = kvp.Value;
            var posX = new AnimationCurve();
            var posY = new AnimationCurve();
            var posZ = new AnimationCurve();
            for (int i = 0; i < curve.positions.Length; i++)
            {
                float time = i * data.frameInterval;
                Vector3 p = curve.positions[i];
                posX.AddKey(time, p.x);
                posY.AddKey(time, p.y);
                posZ.AddKey(time, p.z);
            }
            clip.SetCurve(string.Empty, typeof(Transform), bone + ".localPosition.x", posX);
            clip.SetCurve(string.Empty, typeof(Transform), bone + ".localPosition.y", posY);
            clip.SetCurve(string.Empty, typeof(Transform), bone + ".localPosition.z", posZ);
        }
        return clip;
    }
}
