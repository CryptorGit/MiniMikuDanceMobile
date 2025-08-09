using System.Numerics;

namespace MiniMikuDance.IK;

public class TwoBoneSolver : IIkSolver
{
    private readonly float _length1;
    private readonly float _length2;

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
        dist = System.MathF.Clamp(dist, minReach, maxReach);
        var dir = Vector3.Normalize(toTarget);
        target = rootPos + dir * dist;
        end.Position = target;

        var poleDir = chain.Length > 3 ? chain[3].Position - rootPos : mid.Position - rootPos;
        var planeNormal = Vector3.Normalize(Vector3.Cross(dir, poleDir));
        if (planeNormal.LengthSquared() < 1e-6f)
            planeNormal = Vector3.UnitY;
        var planeTangent = Vector3.Normalize(Vector3.Cross(planeNormal, dir));

        var cos0 = (_length1 * _length1 + dist * dist - _length2 * _length2) / (2 * _length1 * dist);
        cos0 = System.MathF.Clamp(cos0, -1f, 1f);
        var angle0 = System.MathF.Acos(cos0);

        var midPos = rootPos + dir * (System.MathF.Cos(angle0) * _length1) + planeTangent * (System.MathF.Sin(angle0) * _length1);
        mid.Position = midPos;

        root.Rotation = LookRotation(midPos - rootPos, planeNormal);
        mid.Rotation = LookRotation(target - midPos, planeNormal);
        end.Rotation = Quaternion.Identity;
    }

    private static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        forward = Vector3.Normalize(forward);
        up = Vector3.Normalize(up);
        var right = Vector3.Normalize(Vector3.Cross(up, forward));
        var newUp = Vector3.Cross(forward, right);
        var m = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            newUp.X, newUp.Y, newUp.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);
        return Quaternion.CreateFromRotationMatrix(m);
    }
}

