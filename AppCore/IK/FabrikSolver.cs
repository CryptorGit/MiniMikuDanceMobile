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
            chain[i].Rotation = IkMathUtil.LookRotation(forward, Vector3.UnitY);
        }
        chain[^1].Rotation = Quaternion.Identity;
    }
}

