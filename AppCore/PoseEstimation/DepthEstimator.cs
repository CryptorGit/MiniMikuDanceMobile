using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MiniMikuDance.PoseEstimation;

public record struct BoneConstraint(BlazePoseJoint JointA, BlazePoseJoint JointB, float Length);

public class DepthEstimator
{
    private readonly List<BoneConstraint> _constraints;

    private const float ShoulderWidth = 0.35f;
    private const float HipWidth = 0.35f;

    public DepthEstimator(IEnumerable<BoneConstraint>? constraints = null)
    {
        _constraints = constraints?.ToList() ?? new List<BoneConstraint>
        {
            new(BlazePoseJoint.LeftShoulder, BlazePoseJoint.LeftElbow, 0.30f),
            new(BlazePoseJoint.LeftElbow, BlazePoseJoint.LeftWrist, 0.25f),
            new(BlazePoseJoint.RightShoulder, BlazePoseJoint.RightElbow, 0.30f),
            new(BlazePoseJoint.RightElbow, BlazePoseJoint.RightWrist, 0.25f),
            new(BlazePoseJoint.LeftHip, BlazePoseJoint.LeftKnee, 0.40f),
            new(BlazePoseJoint.LeftKnee, BlazePoseJoint.LeftAnkle, 0.40f),
            new(BlazePoseJoint.RightHip, BlazePoseJoint.RightKnee, 0.40f),
            new(BlazePoseJoint.RightKnee, BlazePoseJoint.RightAnkle, 0.40f)
        };
    }

    public JointData[] Reconstruct(JointData[] frames)
    {
        foreach (var frame in frames)
        {
            float scale = EstimateScale(frame);
            ApplyDepth(frame, scale);
        }
        return frames;
    }

    private float EstimateScale(JointData frame)
    {
        float s = 0f;
        int count = 0;
        if (TryGetDistance(frame, BlazePoseJoint.LeftShoulder, BlazePoseJoint.RightShoulder, out var shoulder))
        {
            s += ShoulderWidth / shoulder;
            count++;
        }
        if (TryGetDistance(frame, BlazePoseJoint.LeftHip, BlazePoseJoint.RightHip, out var hip))
        {
            s += HipWidth / hip;
            count++;
        }
        return count > 0 ? s / count : 1f;
    }

    private void ApplyDepth(JointData frame, float scale)
    {
        if (frame.Positions.Length > (int)BlazePoseJoint.RightHip &&
            frame.Positions.Length > (int)BlazePoseJoint.LeftHip)
        {
            frame.Positions[(int)BlazePoseJoint.LeftHip].Z = 0f;
            frame.Positions[(int)BlazePoseJoint.RightHip].Z = 0f;
        }

        foreach (var c in _constraints)
        {
            int ia = (int)c.JointA;
            int ib = (int)c.JointB;
            if (ia >= frame.Positions.Length || ib >= frame.Positions.Length)
                continue;

            var a = frame.Positions[ia];
            var b = frame.Positions[ib];
            float dist2D = Vector2.Distance(new Vector2(a.X, a.Y), new Vector2(b.X, b.Y));
            float target = c.Length * scale;
            if (dist2D < 1e-5f || target <= dist2D)
            {
                b.Z = a.Z;
            }
            else
            {
                float dz = MathF.Sqrt(target * target - dist2D * dist2D);
                b.Z = a.Z + (b.Y < a.Y ? -dz : dz);
            }
            frame.Positions[ib] = b;
        }
    }

    private static bool TryGetDistance(JointData frame, BlazePoseJoint a, BlazePoseJoint b, out float dist)
    {
        int ia = (int)a;
        int ib = (int)b;
        if (ia < frame.Positions.Length && ib < frame.Positions.Length)
        {
            var pa = frame.Positions[ia];
            var pb = frame.Positions[ib];
            dist = Vector2.Distance(new Vector2(pa.X, pa.Y), new Vector2(pb.X, pb.Y));
            return dist > 1e-5f;
        }
        dist = 0f;
        return false;
    }
}
