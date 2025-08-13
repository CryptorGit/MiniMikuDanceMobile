using System;
using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.IK;

public class CcdSolver : IIkSolver
{
    private const float Epsilon = 1e-6f;

    public void Solve(IkBone[] chain, IkLink[] links, int iterations, Func<int, float>? rotationLimitFunc = null)
    {
        if (chain.Length < 2)
            return;

        var target = chain[^1].Position;
        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = chain.Length - 2; i >= 0; i--)
            {
                var jointPos = chain[i].Position;
                var toEffector = chain[^1].Position - jointPos;
                var toTarget = target - jointPos;
                if (toEffector.LengthSquared() < Epsilon || toTarget.LengthSquared() < Epsilon)
                    continue;
                var rot = FromToRotation(toEffector, toTarget);
                var limit = rotationLimitFunc?.Invoke(i) ?? 0f;
                if (limit != 0f)
                    rot = ClampAngle(rot, limit);
                for (int j = i + 1; j < chain.Length; j++)
                {
                    var rel = chain[j].Position - jointPos;
                    rel = Vector3.Transform(rel, rot);
                    chain[j].Position = jointPos + rel;
                }
            }
        }

        var prevRot = chain[0].Rotation;
        for (int i = 0; i < chain.Length - 1; i++)
        {
            var forward = chain[i + 1].Position - chain[i].Position;
            var baseForward = Vector3.Transform(chain[i].BaseForward, prevRot);
            var rotDelta = FromToRotation(baseForward, forward);
            var limit = rotationLimitFunc?.Invoke(i) ?? 0f;
            if (limit != 0f)
                rotDelta = ClampAngle(rotDelta, limit);

            forward = Vector3.Normalize(forward);
            var up = Vector3.Transform(chain[i].BaseUp, prevRot);
            up = Vector3.Transform(up, rotDelta);
            up -= Vector3.Dot(up, forward) * forward;

            if (chain[i].PoleVector.LengthSquared() > Epsilon)
            {
                var pole = Vector3.Normalize(chain[i].PoleVector);
                var poleUp = Vector3.Cross(pole, forward);
                if (poleUp.LengthSquared() > Epsilon)
                    up = poleUp;
            }

            if (up.LengthSquared() < Epsilon)
            {
                up = chain[i].PrevUp;
                up -= Vector3.Dot(up, forward) * forward;
                if (up.LengthSquared() < Epsilon)
                {
                    var plane = Vector3.Transform(chain[i].BasePlaneNormal, prevRot);
                    plane = Vector3.Transform(plane, rotDelta);
                    up = Vector3.Cross(plane, forward);
                }
            }
            if (up.LengthSquared() < Epsilon)
            {
                up = MathF.Abs(Vector3.Dot(forward, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;
                up -= Vector3.Dot(up, forward) * forward;
            }
            up = Vector3.Normalize(up);
            chain[i].PrevUp = up;

            chain[i].Rotation = LookRotation(forward, up);
            if (i < links.Length && links[i].HasLimit)
                ClampRotation(chain, i, links[i]);
            prevRot = chain[i].Rotation;
        }
        chain[^1].Rotation = prevRot;
    }

    private static Quaternion ClampAngle(Quaternion q, float limit)
    {
        var axis = new Vector3(q.X, q.Y, q.Z);
        var len = axis.Length();
        if (len < Epsilon)
            return Quaternion.Identity;
        var angle = 2f * MathF.Acos(Math.Clamp(q.W, -1f, 1f));
        angle = Math.Clamp(angle, -limit, limit);
        return Quaternion.CreateFromAxisAngle(axis / len, angle);
    }

    private static Quaternion FromToRotation(Vector3 from, Vector3 to)
    {
        var f = from.LengthSquared() > Epsilon ? Vector3.Normalize(from) : Vector3.UnitZ;
        var t = to.LengthSquared() > Epsilon ? Vector3.Normalize(to) : Vector3.UnitZ;
        var dot = Vector3.Dot(f, t);
        if (dot > 1f - Epsilon)
            return Quaternion.Identity;
        if (dot < -1f + Epsilon)
        {
            var axis = Vector3.Cross(f, Vector3.UnitX);
            if (axis.LengthSquared() < Epsilon)
                axis = Vector3.Cross(f, Vector3.UnitY);
            axis = Vector3.Normalize(axis);
            return Quaternion.CreateFromAxisAngle(axis, MathF.PI);
        }
        var axisCross = Vector3.Cross(f, t);
        var angle = MathF.Acos(Math.Clamp(dot, -1f, 1f));
        return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axisCross), angle);
    }

    private static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        var right = Vector3.Cross(up, forward);
        IkDebug.LogAxes(forward, up, right);
        var m = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);
        return Quaternion.CreateFromRotationMatrix(m);
    }

    private static void ClampRotation(IkBone[] chain, int index, IkLink link)
    {
        var parent = index > 0 ? chain[index - 1].Rotation : Quaternion.Identity;
        var local = Quaternion.Normalize(Quaternion.Inverse(parent) * chain[index].Rotation);

        float angle = 2f * MathF.Acos(Math.Clamp(local.W, -1f, 1f));
        var s = MathF.Sqrt(1f - local.W * local.W);
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
}
