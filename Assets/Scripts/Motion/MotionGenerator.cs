using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility to convert JointData arrays into MotionData and AnimationClips.
/// This is a minimal placeholder implementation.
/// </summary>
public class MotionGenerator
{
    /// <summary>
    /// Generate an AnimationClip from MotionData. Only position curves are
    /// currently created for the provided bones.
    /// </summary>
    public AnimationClip GenerateClip(MotionData data)
    {
        var clip = new AnimationClip();
        foreach (var kv in data.boneCurves)
        {
            string boneName = kv.Key;
            BoneCurve curve = kv.Value;
            if (curve.positions == null)
                continue;

            var xKeys = new Keyframe[curve.positions.Length];
            var yKeys = new Keyframe[curve.positions.Length];
            var zKeys = new Keyframe[curve.positions.Length];
            for (int i = 0; i < curve.positions.Length; i++)
            {
                float time = i * data.frameInterval;
                Vector3 pos = curve.positions[i];
                xKeys[i] = new Keyframe(time, pos.x);
                yKeys[i] = new Keyframe(time, pos.y);
                zKeys[i] = new Keyframe(time, pos.z);
            }
            clip.SetCurve(boneName, typeof(Transform), "localPosition.x", new AnimationCurve(xKeys));
            clip.SetCurve(boneName, typeof(Transform), "localPosition.y", new AnimationCurve(yKeys));
            clip.SetCurve(boneName, typeof(Transform), "localPosition.z", new AnimationCurve(zKeys));
        }
        clip.name = "GeneratedClip";
        return clip;
    }

    /// <summary>
    /// Convert JointData frames to MotionData. Only the root joint position is
    /// stored for now and rotations are left identity.
    /// </summary>
    public MotionData GenerateData(JointData[] joints)
    {
        var data = new MotionData();
        if (joints == null || joints.Length == 0)
            return data;

        if (joints.Length > 1)
            data.frameInterval = joints[1].timestamp - joints[0].timestamp;
        else
            data.frameInterval = 1f / 30f;

        var rootCurve = new BoneCurve
        {
            positions = new Vector3[joints.Length],
            rotations = new Quaternion[joints.Length]
        };

        for (int i = 0; i < joints.Length; i++)
        {
            rootCurve.positions[i] = joints[i].positions[0];
            rootCurve.rotations[i] = Quaternion.identity;
        }

        data.boneCurves["Root"] = rootCurve;
        return data;
    }
}
