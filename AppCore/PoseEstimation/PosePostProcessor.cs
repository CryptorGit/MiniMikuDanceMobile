using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Util;

namespace MiniMikuDance.PoseEstimation;

public struct JointConstraint
{
    public BlazePoseJoint Parent { get; set; }
    public BlazePoseJoint Joint { get; set; }
    public BlazePoseJoint Child { get; set; }
    public float MinAngle { get; set; }
    public float MaxAngle { get; set; }
}

public class PosePostProcessor
{
    private readonly List<JointConstraint> _constraints = new();

    public PosePostProcessor(string? jsonPath)
    {
        if (!string.IsNullOrEmpty(jsonPath) && File.Exists(jsonPath))
        {
            var loaded = JSONUtil.Load<JointConstraint[]>(jsonPath);
            _constraints.AddRange(loaded);
        }
    }

    public void ApplyConstraints(JointData[] frames)
    {
        foreach (var frame in frames)
        {
            ApplyConstraints(frame);
        }
    }

    private void ApplyConstraints(JointData frame)
    {
        foreach (var c in _constraints)
        {
            int pi = (int)c.Parent;
            int ji = (int)c.Joint;
            int ci = (int)c.Child;
            if (pi >= frame.Positions.Length || ji >= frame.Positions.Length || ci >= frame.Positions.Length)
                continue;
            var parent = frame.Positions[pi];
            var joint = frame.Positions[ji];
            var child = frame.Positions[ci];
            var v1 = parent - joint;
            var v2 = child - joint;
            if (v1.LengthSquared() < 1e-6f || v2.LengthSquared() < 1e-6f)
                continue;
            var n1 = Vector3.Normalize(v1);
            var n2 = Vector3.Normalize(v2);
            float dot = Math.Clamp(Vector3.Dot(n1, n2), -1f, 1f);
            float angle = MathF.Acos(dot);
            float min = MathF.PI * c.MinAngle / 180f;
            float max = MathF.PI * c.MaxAngle / 180f;
            float clamped = Math.Clamp(angle, min, max);
            if (MathF.Abs(clamped - angle) < 1e-5f)
                continue;
            var axis = Vector3.Cross(n2, n1);
            if (axis.LengthSquared() < 1e-6f)
                continue;
            axis = Vector3.Normalize(axis);
            var rot = Quaternion.CreateFromAxisAngle(axis, clamped - angle);
            var newDir = Vector3.Transform(v2, rot);
            frame.Positions[ci] = joint + newDir;
        }
    }
}
