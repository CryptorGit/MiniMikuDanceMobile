using System;
using System.Numerics;

namespace MiniMikuDance.IK;

public class ConstraintJoint
{
    public int BoneIndex { get; }
    public bool HasLimit { get; }
    public Vector3 MinAngle { get; }
    public Vector3 MaxAngle { get; }

    public ConstraintJoint(int boneIndex, bool hasLimit, Vector3 minAngle, Vector3 maxAngle)
    {
        BoneIndex = boneIndex;
        HasLimit = hasLimit;
        MinAngle = minAngle;
        MaxAngle = maxAngle;
    }

    public bool HasUnitXConstraint => HasLimit && MinAngle.Y == 0f && MaxAngle.Y == 0f && MinAngle.Z == 0f && MaxAngle.Z == 0f;
    public bool HasUnitYConstraint => HasLimit && MinAngle.X == 0f && MaxAngle.X == 0f && MinAngle.Z == 0f && MaxAngle.Z == 0f;
    public bool HasUnitZConstraint => HasLimit && MinAngle.X == 0f && MaxAngle.X == 0f && MinAngle.Y == 0f && MaxAngle.Y == 0f;

    public Quaternion Clamp(Quaternion rotation)
    {
        if (!HasLimit)
            return rotation;
        var euler = ToEuler(rotation);
        if (HasUnitXConstraint)
        {
            euler.Y = 0f;
            euler.Z = 0f;
        }
        else if (HasUnitYConstraint)
        {
            euler.X = 0f;
            euler.Z = 0f;
        }
        else if (HasUnitZConstraint)
        {
            euler.X = 0f;
            euler.Y = 0f;
        }
        euler = Vector3.Clamp(euler, MinAngle, MaxAngle);
        return FromEuler(euler);
    }

    private static Vector3 ToEuler(Quaternion q)
    {
        // Yaw-Pitch-Roll order (Y, X, Z)
        double ysqr = q.Y * q.Y;
        double t0 = +2.0 * (q.W * q.X + q.Y * q.Z);
        double t1 = +1.0 - 2.0 * (q.X * q.X + ysqr);
        double roll = Math.Atan2(t0, t1);
        double t2 = +2.0 * (q.W * q.Y - q.Z * q.X);
        t2 = t2 > 1.0 ? 1.0 : t2;
        t2 = t2 < -1.0 ? -1.0 : t2;
        double pitch = Math.Asin(t2);
        double t3 = +2.0 * (q.W * q.Z + q.X * q.Y);
        double t4 = +1.0 - 2.0 * (ysqr + q.Z * q.Z);
        double yaw = Math.Atan2(t3, t4);
        return new Vector3((float)pitch, (float)yaw, (float)roll);
    }

    private static Quaternion FromEuler(Vector3 e)
    {
        return Quaternion.CreateFromYawPitchRoll(e.Y, e.X, e.Z);
    }
}
