using System.Numerics;
using MiniMikuDance.Util;

namespace MiniMikuDance.IK;

public class FullBodyIkSolver : IIkSolver
{
    private readonly Segment[] _segments;

    private record Segment(IIkSolver Solver, IkBone[] Chain, float[] Lengths);

    public FullBodyIkSolver(params (IIkSolver solver, IkBone[] chain)[] segments)
    {
        _segments = new Segment[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            var (solver, chain) = segments[i];
            var lengths = new float[chain.Length - 1];
            for (int j = 0; j < lengths.Length; j++)
                lengths[j] = Vector3.Distance(chain[j + 1].Position, chain[j].Position);
            _segments[i] = new Segment(solver, chain, lengths);
        }
    }

    public void Solve(IkBone[] chain)
    {
        _ = chain; // Full body solver manages segments internally.
        foreach (var segment in _segments)
        {
            segment.Solver.Solve(segment.Chain);
            for (int i = 0; i < segment.Lengths.Length; i++)
            {
                var to = segment.Chain[i + 1].Position;
                var from = segment.Chain[i].Position;
                IkMath.KeepLength(ref to, ref from, segment.Lengths[i]);
                segment.Chain[i + 1].Position = to;
            }

            if (segment.Chain.Length > 1)
            {
                var dir = segment.Chain[1].Position - segment.Chain[0].Position;
                if (IkMath.SafeNormalize(ref dir))
                {
                    var m = Matrix4x4.CreateLookAt(Vector3.Zero, dir, Vector3.UnitY);
                    segment.Chain[0].Rotation = Quaternion.CreateFromRotationMatrix(m);
                }
            }
        }
    }
}
