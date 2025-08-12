using System;
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

        root.Rotation = LookRotation(midPos - rootPos, planeNormal);
        mid.Rotation = LookRotation(target - midPos, planeNormal);
        ClampBone(root);
        ClampBone(mid);
        end.Rotation = Quaternion.Identity;
    }

    private static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        if (forward.LengthSquared() < Epsilon || up.LengthSquared() < Epsilon)
            return Quaternion.Identity;
        forward = Vector3.Normalize(forward);
        up = Vector3.Normalize(up);
        var right = Vector3.Cross(forward, up);
        if (right.LengthSquared() < Epsilon)
            return Quaternion.Identity;
        right = Vector3.Normalize(right);
        var newUp = Vector3.Cross(right, forward);
        var m = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            newUp.X, newUp.Y, newUp.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);
        return Quaternion.CreateFromRotationMatrix(m);
    }

    private static void ClampBone(IkBone bone)
    {
        if (bone.LowerLimit == null || bone.UpperLimit == null)
            return;
        var delta = Quaternion.Inverse(bone.BaseRotation) * bone.Rotation;
        var euler = ToEuler(delta);
        var lower = bone.LowerLimit.Value;
        var upper = bone.UpperLimit.Value;
        var clamped = new Vector3(
            System.Math.Clamp(euler.X, lower.X, upper.X),
            System.Math.Clamp(euler.Y, lower.Y, upper.Y),
            System.Math.Clamp(euler.Z, lower.Z, upper.Z));
        bone.Rotation = bone.BaseRotation * FromEuler(clamped);
    }

    private static Quaternion FromEuler(Vector3 rad)
    {
        var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rad.Z);
        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rad.X);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rad.Y);
        return qy * qx * qz;
    }

    private static Vector3 ToEuler(Quaternion q)
    {
        var m = Matrix4x4.CreateFromQuaternion(q);
        float sx = -m.M23;
        float cx = MathF.Sqrt(1 - sx * sx);
        float x, y, z;
        if (cx > Epsilon)
        {
            x = MathF.Asin(sx);
            y = MathF.Atan2(m.M13, m.M33);
            z = MathF.Atan2(m.M21, m.M22);
        }
        else
        {
            x = MathF.Asin(sx);
            y = MathF.Atan2(-m.M31, m.M11);
            z = 0f;
        }
        return new Vector3(x, y, z);
    }
}

