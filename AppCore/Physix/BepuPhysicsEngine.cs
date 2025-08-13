using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using MiniMikuDance.Data;
using System.Numerics;

namespace MiniMikuDance.Physix;

public sealed class BepuPhysicsEngine : IPhysicsEngine, IDisposable
{
    private static readonly BufferPool SharedBufferPool = new();
    private Simulation _simulation = null!;

    public void Setup(MmdModel model)
    {
        _simulation?.Dispose();
        _simulation = Simulation.Create(SharedBufferPool, new NarrowPhaseCallbacks(),
            new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0)), new SolveDescription(8, 1));
        // TODO: MmdModel から剛体・ジョイントを生成する
    }

    public void Step(float deltaTime)
    {
        const float step = 1f / 60f;
        var substepCount = Math.Max(1, (int)MathF.Ceiling(deltaTime / step));
        var substepDt = deltaTime / substepCount;
        for (var i = 0; i < substepCount; i++)
        {
            _simulation.Timestep(substepDt);
        }
    }

    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RayHit hit)
    {
        var handler = new ClosestHitHandler();
        _simulation.RayCast(origin, direction, maxDistance, ref handler);
        if (handler.Hit)
        {
            hit = new RayHit
            {
                HasHit = true,
                Position = origin + direction * handler.T,
                Normal = handler.Normal,
                Distance = handler.T
            };
            return true;
        }
        hit = default;
        return false;
    }

    public void Dispose()
    {
        _simulation.Dispose();
    }

    private struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        public void Initialize(Simulation simulation) { }
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
            => a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;
        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial.FrictionCoefficient = 1f;
            pairMaterial.MaximumRecoveryVelocity = 2f;
            pairMaterial.SpringSettings = new SpringSettings(30f, 1f);
            return true;
        }
        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;
        public void Dispose() { }
    }

    private struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        private Vector3 _gravity;
        private Vector3Wide _gravityDt;
        public PoseIntegratorCallbacks(Vector3 gravity) : this() => _gravity = gravity;
        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
        public bool AllowSubstepsForUnconstrainedBodies => false;
        public bool IntegrateVelocityForKinematics => false;
        public void Initialize(Simulation simulation) { }
        public void PrepareForIntegration(float dt) => _gravityDt = Vector3Wide.Broadcast(_gravity * dt);
        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            velocity.Linear += _gravityDt;
        }
    }

    private struct ClosestHitHandler : IRayHitHandler
    {
        public bool Hit;
        public float T;
        public Vector3 Normal;
        public bool AllowTest(CollidableReference collidable) => true;
        public bool AllowTest(CollidableReference collidable, int childIndex) => true;
        public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
        {
            Hit = true;
            T = t;
            Normal = normal;
            maximumT = t;
        }
    }
}
