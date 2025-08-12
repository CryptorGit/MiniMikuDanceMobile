using System;
using System.Numerics;

namespace MiniMikuDance.IK;

public class FabrikSolver : IIkSolver
{
    private readonly float[] _lengths;
    private readonly int _iterations;
    private const float Epsilon = 1e-6f;

    public FabrikSolver(float[] lengths, int iterations = 10)
    {
        _lengths = lengths;
        _iterations = iterations;
    }

    public void Solve(IkBone[] chain)
    {
        if (chain.Length != _lengths.Length + 1)
            return;

        var target = chain[^1].Position;
        var rootPos = chain[0].Position;
        var total = 0f;
        foreach (var l in _lengths)
            total += l;

        if (Vector3.Distance(rootPos, target) > total)
        {
            var diff = target - rootPos;
            var dir = diff.LengthSquared() > Epsilon ? Vector3.Normalize(diff) : Vector3.UnitX;
            chain[0].Position = rootPos;
            for (int i = 1; i < chain.Length; i++)
            {
                chain[i].Position = chain[i - 1].Position + dir * _lengths[i - 1];
            }
        }
        else
        {
            var basePos = rootPos;
            for (int iter = 0; iter < _iterations; iter++)
            {
                chain[^1].Position = target;
                for (int i = chain.Length - 2; i >= 0; i--)
                {
                    var delta = chain[i].Position - chain[i + 1].Position;
                    var dir = delta.LengthSquared() > Epsilon ? Vector3.Normalize(delta) : Vector3.UnitX;
                    chain[i].Position = chain[i + 1].Position + dir * _lengths[i];
                }

                chain[0].Position = basePos;
                for (int i = 1; i < chain.Length; i++)
                {
                    var delta = chain[i].Position - chain[i - 1].Position;
                    var dir = delta.LengthSquared() > Epsilon ? Vector3.Normalize(delta) : Vector3.UnitX;
                    chain[i].Position = chain[i - 1].Position + dir * _lengths[i - 1];
                }
            }
        }

        for (int i = 0; i < chain.Length - 1; i++)
        {
            var forward = chain[i + 1].Position - chain[i].Position;
            chain[i].Rotation = LookRotation(forward, Vector3.UnitY);
            ClampBone(chain[i]);
        }
        chain[^1].Rotation = Quaternion.Identity;
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

