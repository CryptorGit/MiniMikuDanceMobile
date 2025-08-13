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

        _ = iterations;

        var target = chain[^1].Position;
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

            chain[i].Rotation = IkMath.LookRotation(forward, up);
            if (i < links.Length && links[i].HasLimit)
                RotationConstraints.ClampRotation(chain, i, links[i]);
            RotationConstraints.ApplyRoleConstraint(chain, i);
            prevRot = chain[i].Rotation;
        }
        chain[^1].Rotation = prevRot;
        RotationConstraints.ApplyRoleConstraint(chain, chain.Length - 1);
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

}
