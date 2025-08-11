using System.Numerics;

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

    public void Solve(IkBone[] chain)
    {
        if (chain.Length < 3)
            return;

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

        var poleDir = chain.Length > 3 ? chain[3].Position - rootPos : mid.Position - rootPos;
        var cross = Vector3.Cross(poleDir, dir);
        var planeNormal = cross.LengthSquared() > Epsilon ? Vector3.Normalize(cross) : Vector3.UnitY;
        var tangentCross = Vector3.Cross(planeNormal, dir);
        var planeTangent = tangentCross.LengthSquared() > Epsilon ? Vector3.Normalize(tangentCross) : Vector3.UnitX;

        var cos0 = (_length1 * _length1 + dist * dist - _length2 * _length2) / (2 * _length1 * dist);
        cos0 = System.Math.Clamp(cos0, -1f, 1f);
        var angle0 = System.MathF.Acos(cos0);

        var midPos = rootPos + dir * (System.MathF.Cos(angle0) * _length1) + planeTangent * (System.MathF.Sin(angle0) * _length1);
        mid.Position = midPos;

        root.Rotation = LookRotation(midPos - rootPos, root.BaseUp);
        mid.Rotation = LookRotation(target - midPos, mid.BaseUp);
        end.Rotation = Quaternion.Identity;
    }

    private static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        if (forward.LengthSquared() < Epsilon || up.LengthSquared() < Epsilon)
            return Quaternion.Identity;
        forward = Vector3.Normalize(forward);
        up = Vector3.Normalize(up);
        var right = Vector3.Cross(up, forward);
        if (right.LengthSquared() < Epsilon)
        {
            up = System.MathF.Abs(Vector3.Dot(forward, Vector3.UnitY)) < 0.99f ? Vector3.UnitY : Vector3.UnitZ;
            right = Vector3.Cross(up, forward);
        }
        right = Vector3.Normalize(right);
        up = Vector3.Normalize(Vector3.Cross(forward, right));
        var m = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);
        return Quaternion.CreateFromRotationMatrix(m);
    }
}

