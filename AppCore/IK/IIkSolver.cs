using MiniMikuDance.Import;

namespace MiniMikuDance.IK;

public interface IIkSolver
{
    void Solve(IkBone[] chain, IkLink[] links, int iterations);
}

