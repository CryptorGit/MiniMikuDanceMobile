using System;
using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.IK;

public class TwoBoneSolver : IIkSolver
{
    private readonly float _length1;
    private readonly float _length2;
    private const float Epsilon = 1e-6f;

    public TwoBoneSolver(float length1, float length2)
    {
        _length1 = length1;
        _length2 = length2;
    }

    public void Solve(IkBone[] chain, IkLink[] links, int iterations, Func<int, float>? rotationLimitFunc = null)
    {
        if (chain.Length < 3)
            return;

        _ = iterations;

        var root = chain[0];
        var mid = chain[1];
        var end = chain[2];

        var target = end.Position;
        var rootPos = root.Position;

        var toTarget = target - rootPos;
        var dist = toTarget.Length();
        var maxReach = _length1 + _length2 - 1e-5f;
        var minReach = System.MathF.Abs(_length1 - _length2) + 1e-5f;
        dist = System.Math.Clamp(dist, minReach, maxReach);
        var dir = toTarget.LengthSquared() > Epsilon ? Vector3.Normalize(toTarget) : Vector3.UnitX;
        target = rootPos + dir * dist;
        end.Position = target;

        var planeNormal = root.PoleVector.LengthSquared() > Epsilon
            ? Vector3.Normalize(root.PoleVector)
            : Vector3.Normalize(Vector3.Cross(mid.BasePosition - root.BasePosition, end.BasePosition - root.BasePosition));
        var planeTangent = Vector3.Normalize(Vector3.Cross(dir, planeNormal));

        var cos0 = (_length1 * _length1 + dist * dist - _length2 * _length2) / (2 * _length1 * dist);
        cos0 = System.Math.Clamp(cos0, -1f, 1f);
        var angle0 = System.MathF.Acos(cos0);
        var limit0 = rotationLimitFunc?.Invoke(0) ?? 0f;
        if (limit0 != 0f)
            angle0 = Math.Clamp(angle0, -limit0, limit0);

        var cos1 = (_length1 * _length1 + _length2 * _length2 - dist * dist) / (2 * _length1 * _length2);
        var angle1 = MathF.Acos(Math.Clamp(cos1, -1f, 1f));
        var limit1 = rotationLimitFunc?.Invoke(1) ?? 0f;
        if (limit1 != 0f)
            angle1 = Math.Clamp(angle1, -limit1, limit1);
        var midDir = dir * System.MathF.Cos(angle0) + planeTangent * System.MathF.Sin(angle0);
        var bendDir = dir * System.MathF.Cos(angle1) - planeTangent * System.MathF.Sin(angle1);

        mid.Position = rootPos + midDir * _length1;
        end.Position = mid.Position + bendDir * _length2;

        root.Rotation = IkMath.LookRotation(midDir, planeNormal);
        mid.Rotation = IkMath.LookRotation(bendDir, planeNormal);
        end.Rotation = IkMath.LookRotation(bendDir, planeNormal);

        if (links.Length > 0 && links[0].HasLimit)
            ClampRotation(chain, 0, links[0]);
        if (links.Length > 1 && links[1].HasLimit)
            ClampRotation(chain, 1, links[1]);
        if (links.Length > 2 && links[2].HasLimit)
            ClampRotation(chain, 2, links[2]);
        for (int i = 0; i < chain.Length; i++)
            ApplyRoleConstraint(chain, i);
    }

    private static void ClampRotation(IkBone[] chain, int index, IkLink link)
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

    private static float ClampAxis(float angle, float min, float max)
    {
        return Math.Clamp(NormalizeAngle(angle), min, max);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle < -MathF.PI) angle += 2f * MathF.PI;
        while (angle > MathF.PI) angle -= 2f * MathF.PI;
        return angle;
    }

    private static void ApplyRoleConstraint(IkBone[] chain, int index)
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
}

