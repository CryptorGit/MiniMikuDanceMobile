using System.Numerics;

namespace MiniMikuDance.Physics;

public struct PhysicsConfig
{
    public Vector3 Gravity;
    public int SolverIterationCount;
    public int SubstepCount;

    public PhysicsConfig(Vector3 gravity, int solverIterationCount, int substepCount)
    {
        Gravity = gravity;
        SolverIterationCount = solverIterationCount;
        SubstepCount = substepCount;
    }
}

