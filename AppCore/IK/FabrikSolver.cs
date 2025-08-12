using System.Numerics;
using MiniMikuDance.Import;
using MiniMikuDance.Util;

namespace MiniMikuDance.IK;

public class FabrikSolver : IIkSolver
{
    private readonly float[] _lengths;
    private const float Epsilon = 1e-6f;

    public FabrikSolver(float[] lengths)
    {
        _lengths = lengths;
    }

    public void Solve(IkBone[] chain, IkLink[] links, int iterations)
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
            for (int iter = 0; iter < iterations; iter++)
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

        var prevRot = chain[0].Rotation;
        for (int i = 0; i < chain.Length - 1; i++)
        {
            var forward = chain[i + 1].Position - chain[i].Position;
            var baseForward = Vector3.Transform(chain[i].BaseForward, prevRot);
            var rotDelta = FromToRotation(baseForward, forward);
            var up = Vector3.Transform(chain[i].BaseUp, prevRot);
            up = Vector3.Transform(up, rotDelta);
            chain[i].Rotation = LookRotation(forward, up);
            if (i < links.Length && links[i].HasLimit)
                ClampRotation(chain, i, links[i]);
            prevRot = chain[i].Rotation;
        }
        chain[^1].Rotation = prevRot;
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
            var axis = Vector3.Cross(Vector3.UnitX, f);
            if (axis.LengthSquared() < Epsilon)
                axis = Vector3.Cross(Vector3.UnitY, f);
            axis = Vector3.Normalize(axis);
            return Quaternion.CreateFromAxisAngle(axis, MathF.PI);
        }
        var axisCross = Vector3.Cross(f, t);
        var angle = MathF.Acos(Math.Clamp(dot, -1f, 1f));
        return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axisCross), angle);
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

