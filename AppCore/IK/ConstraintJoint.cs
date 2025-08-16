using System;
using System.Numerics;
using MiniMikuDance.Util;

namespace MiniMikuDance.IK;

public class ConstraintJoint
{
    public int BoneIndex { get; }
    public bool HasLimit { get; }
    public Vector3 MinAngle { get; }
    public Vector3 MaxAngle { get; }
    public bool HasUnitXConstraint { get; }

    public ConstraintJoint(int boneIndex, string name, bool hasLimit, Vector3 minAngle, Vector3 maxAngle)
    {
        BoneIndex = boneIndex;
        HasLimit = hasLimit;
        MinAngle = minAngle;
        MaxAngle = maxAngle;
        HasUnitXConstraint = name.Contains("ひざ") || name.Contains("膝");
    }

    public Quaternion ApplyLimit(Quaternion rotation)
    {
        if (HasUnitXConstraint)
        {
            var euler = rotation.ToEulerRadians();
            float x = Math.Clamp(euler.X, MinAngle.X, MaxAngle.X);
            return Quaternion.CreateFromAxisAngle(Vector3.UnitX, x);
        }
        if (HasLimit)
        {
            var euler = rotation.ToEulerRadians();
            var clamped = new Vector3(
                Math.Clamp(euler.X, MinAngle.X, MaxAngle.X),
                Math.Clamp(euler.Y, MinAngle.Y, MaxAngle.Y),
                Math.Clamp(euler.Z, MinAngle.Z, MaxAngle.Z));
            return Quaternion.CreateFromYawPitchRoll(clamped.Y, clamped.X, clamped.Z);
        }
        return rotation;
    }
}
