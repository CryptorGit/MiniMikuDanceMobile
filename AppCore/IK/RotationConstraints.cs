using System;
using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.IK;

internal static class RotationConstraints
{
    private const float Epsilon = 1e-6f;

    public static void ClampRotation(IkBone[] chain, int index, IkLink link)
    {
        var parent = index > 0 ? chain[index - 1].Rotation : Quaternion.Identity;
        var local = Quaternion.Normalize(Quaternion.Inverse(parent) * chain[index].Rotation);

        local.W = Math.Clamp(local.W, -1f, 1f);
        float angle = 2f * MathF.Acos(local.W);
        var s = MathF.Sqrt(MathF.Max(0f, 1f - local.W * local.W));
        Vector3 axis;
        if (s < Epsilon)
            axis = new Vector3(1f, 0f, 0f);
        else
            axis = new Vector3(local.X, local.Y, local.Z) / s;
        if (angle > MathF.PI)
        {
            angle -= 2f * MathF.PI;
            axis = -axis;
        }
        var rot = axis * angle;

        rot.X = ClampAxis(rot.X, link.MinAngle.X, link.MaxAngle.X);
        rot.Y = ClampAxis(rot.Y, link.MinAngle.Y, link.MaxAngle.Y);
        rot.Z = ClampAxis(rot.Z, link.MinAngle.Z, link.MaxAngle.Z);

        var clampedAngle = rot.Length();
        Quaternion clampedQuat = clampedAngle < Epsilon
            ? Quaternion.Identity
            : Quaternion.CreateFromAxisAngle(Vector3.Normalize(rot), clampedAngle);

        chain[index].Rotation = parent * clampedQuat;
    }

    public static void ApplyRoleConstraint(IkBone[] chain, int index)
    {
        var role = chain[index].Role;
        if (role == BoneRole.None)
            return;

        var parent = index > 0 ? chain[index - 1].Rotation : Quaternion.Identity;
        var local = Quaternion.Normalize(Quaternion.Inverse(parent) * chain[index].Rotation);

        local.W = Math.Clamp(local.W, -1f, 1f);
        float angle = 2f * MathF.Acos(local.W);
        var s = MathF.Sqrt(MathF.Max(0f, 1f - local.W * local.W));
        Vector3 axis = s < Epsilon ? new Vector3(1f, 0f, 0f) : new Vector3(local.X, local.Y, local.Z) / s;
        if (angle > MathF.PI)
        {
            angle -= 2f * MathF.PI;
            axis = -axis;
        }
        var rot = axis * angle;

        switch (role)
        {
            case BoneRole.Ankle:
                rot.Z = 0f;
                break;
            case BoneRole.Knee:
                rot.X = MathF.Max(rot.X, 0f);
                break;
        }

        var clampedAngle = rot.Length();
        Quaternion clampedQuat = clampedAngle < Epsilon
            ? Quaternion.Identity
            : Quaternion.CreateFromAxisAngle(Vector3.Normalize(rot), clampedAngle);

        chain[index].Rotation = parent * clampedQuat;
    }

    public static float NormalizeAngle(float angle)
    {
        while (angle < -MathF.PI) angle += 2f * MathF.PI;
        while (angle > MathF.PI) angle -= 2f * MathF.PI;
        return angle;
    }

    private static float ClampAxis(float angle, float min, float max)
    {
        return Math.Clamp(NormalizeAngle(angle), min, max);
    }
}

