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

        var target = chain[^1].Position;
        for (var t = 0; t < iterations; t++)
        {
            var root = chain[0];
            var mid = chain[1];
            var end = chain[2];

            var rootPos = root.Position;

            var toTarget = target - rootPos;
            var dist = toTarget.Length();
            var maxReach = _length1 + _length2 - 1e-5f;
            var minReach = System.MathF.Abs(_length1 - _length2) + 1e-5f;
            dist = System.Math.Clamp(dist, minReach, maxReach);
            var dir = toTarget.LengthSquared() > Epsilon ? Vector3.Normalize(toTarget) : Vector3.UnitX;
            var currentTarget = rootPos + dir * dist;
            end.Position = currentTarget;

            var planeNormal = root.PoleVector.LengthSquared() > Epsilon
                ? Vector3.Normalize(root.PoleVector)
                : Vector3.Normalize(Vector3.Cross(mid.BasePosition - root.BasePosition, end.BasePosition - root.BasePosition));
            var planeTangent = Vector3.Cross(dir, planeNormal);
            if (planeTangent.LengthSquared() <= Epsilon)
            {
                var projectedPole = root.PoleVector - dir * Vector3.Dot(root.PoleVector, dir);
                var fallback = projectedPole.LengthSquared() > Epsilon
                    ? Vector3.Normalize(projectedPole)
                    : (System.MathF.Abs(dir.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX);
                planeTangent = Vector3.Normalize(Vector3.Cross(dir, fallback));
                planeNormal = Vector3.Normalize(Vector3.Cross(planeTangent, dir));
            }
            else
            {
                planeTangent = Vector3.Normalize(planeTangent);
            }

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
            end.Rotation = IkMath.LookRotation(dir, planeNormal);

            if (links.Length > 0 && links[0].HasLimit)
                RotationConstraints.ClampRotation(chain, 0, links[0]);
            if (links.Length > 1 && links[1].HasLimit)
                RotationConstraints.ClampRotation(chain, 1, links[1]);
            if (links.Length > 2 && links[2].HasLimit)
                RotationConstraints.ClampRotation(chain, 2, links[2]);
            for (int i = 0; i < chain.Length; i++)
                RotationConstraints.ApplyRoleConstraint(chain, i);

            if ((end.Position - target).LengthSquared() < Epsilon)
                break;
        }
    }

}

