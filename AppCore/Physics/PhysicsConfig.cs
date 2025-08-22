using System.Numerics;

namespace MiniMikuDance.AppCore.Physics;

public struct PhysicsConfig
{
    public Vector3 Gravity { get; set; }
    public int SolverIterationCount { get; set; }
    public int SubstepCount { get; set; }
    public float Damping { get; set; }
    public float BoneBlendFactor { get; set; }
    public float GroundHeight { get; set; }
    public float Restitution { get; set; }
    public float Friction { get; set; }

    public PhysicsConfig(Vector3 gravity, int solverIterationCount, int substepCount, float damping = 0.98f, float boneBlendFactor = 0.5f, float groundHeight = 0f, float restitution = 0.2f, float friction = 0.5f)
    {
        Gravity = gravity;
        SolverIterationCount = solverIterationCount;
        SubstepCount = substepCount;
        Damping = damping;
        BoneBlendFactor = boneBlendFactor;
        GroundHeight = groundHeight;
        Restitution = restitution;
        Friction = friction;
    }
}

