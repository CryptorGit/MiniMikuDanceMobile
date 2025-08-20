using System.Numerics;

namespace MiniMikuDance.Physics;

public struct PhysicsConfig
{
    public Vector3 Gravity { get; set; }
    public int SolverIterationCount { get; set; }
    public int SubstepCount { get; set; }
    public float Damping { get; set; }

    public PhysicsConfig(Vector3 gravity, int solverIterationCount, int substepCount, float damping = 0.98f)
    {
        Gravity = gravity;
        SolverIterationCount = solverIterationCount;
        SubstepCount = substepCount;
        Damping = damping;
    }
}

