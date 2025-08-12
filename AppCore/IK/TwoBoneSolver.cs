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

    public void Solve(IkBone[] chain, IkLink[] links, int iterations, float rotationLimit = 0f)
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

            var planeNormal = root.PoleVector.LengthSquared() > Epsilon ? Vector3.Normalize(root.PoleVector) : Vector3.UnitY;
            var tangentCross = Vector3.Cross(dir, planeNormal);
            var planeTangent = tangentCross.LengthSquared() > Epsilon ? Vector3.Normalize(tangentCross) : Vector3.UnitX;

            var cos0 = (_length1 * _length1 + dist * dist - _length2 * _length2) / (2 * _length1 * dist);
            cos0 = System.Math.Clamp(cos0, -1f, 1f);
            var angle0 = System.MathF.Acos(cos0);
            if (rotationLimit != 0f)
                angle0 = Math.Clamp(angle0, -rotationLimit, rotationLimit);

            var cos1 = (_length1 * _length1 + _length2 * _length2 - dist * dist) / (2 * _length1 * _length2);
            var angle1 = MathF.Acos(Math.Clamp(cos1, -1f, 1f));
            if (rotationLimit != 0f)
                angle1 = Math.Clamp(angle1, -rotationLimit, rotationLimit);
            var midDir = dir * System.MathF.Cos(angle0) + planeTangent * System.MathF.Sin(angle0);
            var bendDir = dir * System.MathF.Cos(angle1) - planeTangent * System.MathF.Sin(angle1);

            mid.Position = rootPos + midDir * _length1;
            end.Position = mid.Position + bendDir * _length2;

            root.Rotation = LookRotation(midDir, planeNormal);
            mid.Rotation = LookRotation(bendDir, planeNormal);
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

