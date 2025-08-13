using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using MiniMikuDance.Data;
using System.Numerics;
using System.Collections.Generic;

namespace MiniMikuDance.Physix;

public sealed class BepuPhysicsEngine : IPhysicsEngine, IDisposable
{
    private static readonly BufferPool SharedBufferPool = new();
    private Simulation _simulation = null!;
    private float _timeAccumulator;

    public void Setup(MmdModel model)
    {
        _simulation?.Dispose();
        _simulation = Simulation.Create(SharedBufferPool, new NarrowPhaseCallbacks(),
            new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0)), new SolveDescription(8, 1));

        var bodyHandles = new List<BodyHandle>(model.RigidBodies.Count);
        foreach (var rb in model.RigidBodies)
        {
            CollidableDescription collidable;
            BodyInertia inertia;
            switch (rb.Shape)
            {
                case Import.RigidBodyShape.Sphere:
                    var sphere = new Sphere(rb.Size.X);
                    collidable = new CollidableDescription(_simulation.Shapes.Add(sphere));
                    inertia = sphere.ComputeInertia(rb.Mass);
                    break;
                case Import.RigidBodyShape.Box:
                    var box = new Box(rb.Size.X, rb.Size.Y, rb.Size.Z);
                    collidable = new CollidableDescription(_simulation.Shapes.Add(box));
                    inertia = box.ComputeInertia(rb.Mass);
                    break;
                case Import.RigidBodyShape.Capsule:
                    var capsule = new Capsule(rb.Size.X, rb.Size.Y);
                    collidable = new CollidableDescription(_simulation.Shapes.Add(capsule));
                    inertia = capsule.ComputeInertia(rb.Mass);
                    break;
                default:
                    continue;
            }

            var description = BodyDescription.CreateDynamic(new RigidPose(Vector3.Zero), inertia, collidable, new BodyActivityDescription(0.01f));
            var handle = _simulation.Bodies.Add(description);
            bodyHandles.Add(handle);
        }

        foreach (var j in model.Joints)
        {
            if (j.RigidBodyA < 0 || j.RigidBodyB < 0 || j.RigidBodyA >= bodyHandles.Count || j.RigidBodyB >= bodyHandles.Count)
                continue;
            var ha = bodyHandles[j.RigidBodyA];
            var hb = bodyHandles[j.RigidBodyB];

            var linearSpring = new SpringSettings(j.LinearSpring.Frequency, j.LinearSpring.DampingRatio);
            _simulation.Solver.Add(ha, hb, new LinearAxisLimit
            {
                LocalAxis = Vector3.UnitX,
                MinimumOffset = j.LinearLowerLimit.X,
                MaximumOffset = j.LinearUpperLimit.X,
                SpringSettings = linearSpring
            });
            _simulation.Solver.Add(ha, hb, new LinearAxisLimit
            {
                LocalAxis = Vector3.UnitY,
                MinimumOffset = j.LinearLowerLimit.Y,
                MaximumOffset = j.LinearUpperLimit.Y,
                SpringSettings = linearSpring
            });
            _simulation.Solver.Add(ha, hb, new LinearAxisLimit
            {
                LocalAxis = Vector3.UnitZ,
                MinimumOffset = j.LinearLowerLimit.Z,
                MaximumOffset = j.LinearUpperLimit.Z,
                SpringSettings = linearSpring
            });

            var angularSpring = new SpringSettings(j.AngularSpring.Frequency, j.AngularSpring.DampingRatio);
            _simulation.Solver.Add(ha, hb, new AngularServo
            {
                TargetRelativeRotationLocalA = Quaternion.Identity,
                SpringSettings = angularSpring,
                ServoSettings = ServoSettings.Default
            });
            // TODO: Angular limits are not yet implemented
        }
    }

    public void Step(float deltaTime)
    {
        const float step = 1f / 60f;
        _timeAccumulator += deltaTime;
        while (_timeAccumulator >= step)
        {
            _simulation.Timestep(step);
            _timeAccumulator -= step;
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
