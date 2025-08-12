using System.Numerics;
using MiniMikuDance.Import;
using MiniMikuDance.Util;

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

    public void Solve(IkBone[] chain, IkLink[] links, int iterations)
    {
        if (chain.Length < 3)
            return;

        var root = chain[0];
        var mid = chain[1];
        var end = chain[2];

        iterations = System.Math.Max(1, iterations);
        for (int iter = 0; iter < iterations; iter++)
        {
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

            var planeNormal = root.PoleVector;
            if (planeNormal.LengthSquared() < Epsilon)
            {
                var cross = Vector3.Cross(mid.Position - rootPos, dir);
                planeNormal = cross.LengthSquared() > Epsilon ? Vector3.Normalize(cross) : Vector3.UnitY;
            }
            else
            {
                planeNormal = Vector3.Normalize(planeNormal);
            }
            var tangentCross = Vector3.Cross(planeNormal, dir);
            var planeTangent = tangentCross.LengthSquared() > Epsilon ? Vector3.Normalize(tangentCross) : Vector3.UnitX;

            var cos0 = (_length1 * _length1 + dist * dist - _length2 * _length2) / (2 * _length1 * dist);
            cos0 = System.Math.Clamp(cos0, -1f, 1f);
            var angle0 = System.MathF.Acos(cos0);

            var midPos = rootPos + dir * (System.MathF.Cos(angle0) * _length1) + planeTangent * (System.MathF.Sin(angle0) * _length1);
            mid.Position = midPos;

            root.Rotation = LookRotation(midPos - rootPos, planeNormal);
            mid.Rotation = LookRotation(target - midPos, planeNormal);
        }

        end.Rotation = Quaternion.Identity;
        if (links.Length > 0 && links[0].HasLimit)
            ClampRotation(chain, 0, links[0]);
        if (links.Length > 1 && links[1].HasLimit)
            ClampRotation(chain, 1, links[1]);
    }

    private static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        if (forward.LengthSquared() < Epsilon || up.LengthSquared() < Epsilon)
            return Quaternion.Identity;
        forward = Vector3.Normalize(forward);
        var proj = Vector3.Dot(up, forward);
        up -= proj * forward;
        if (up.LengthSquared() < Epsilon)
            return Quaternion.Identity;
        up = Vector3.Normalize(up);
        var right = Vector3.Cross(up, forward);
        if (right.LengthSquared() < Epsilon)
            return Quaternion.Identity;
        right = Vector3.Normalize(right);
        var newUp = Vector3.Cross(forward, right);
        IkDebug.LogAxes(forward, newUp, right);
        var m = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            newUp.X, newUp.Y, newUp.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);
        return Quaternion.CreateFromRotationMatrix(m);
    }

    private static void ClampRotation(IkBone[] chain, int index, IkLink link)
    {
        var parent = index > 0 ? chain[index - 1].Rotation : Quaternion.Identity;
        var local = Quaternion.Inverse(parent) * chain[index].Rotation;
        var euler = local.ToEulerDegrees() * (MathF.PI / 180f);
        var clamped = Vector3.Clamp(euler, link.MinAngle, link.MaxAngle);
        var deg = clamped * (180f / MathF.PI);
        chain[index].Rotation = parent * deg.FromEulerDegrees();
    }
}

