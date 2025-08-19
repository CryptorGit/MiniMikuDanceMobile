using System.Numerics;
using System.Collections.Generic;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using MiniMikuDance.Import;

namespace MiniMikuDance.Physics;

public sealed class BepuPhysicsWorld : IPhysicsWorld
{
    private BufferPool? _bufferPool;
    private Simulation? _simulation;
    private readonly Dictionary<BodyHandle, int> _bodyBoneMap = new();

    public void Initialize()
    {
        _bufferPool = new BufferPool();
        _simulation = Simulation.Create(_bufferPool,
            new SimpleNarrowPhaseCallbacks(),
            new SimplePoseIntegratorCallbacks(new Vector3(0, -9.81f, 0)),
            new SolveDescription(8, 1));
    }

    public void Step(float dt)
    {
        _simulation?.Timestep(dt);
    }

    public void Dispose()
    {
        _simulation?.Dispose();
        _simulation = null;

        _bufferPool?.Clear();
        _bufferPool = null;
    }

    public void LoadRigidBodies(ModelData model)
    {
        if (_simulation is null) return;
        foreach (var rb in model.RigidBodies)
        {
            TypedIndex shapeIndex;
            BodyInertia inertia = default;
            switch (rb.Shape)
            {
                case RigidBodyShape.Sphere:
                    var sphere = new Sphere(rb.Size.X);
                    shapeIndex = _simulation.Shapes.Add(sphere);
                    inertia = sphere.ComputeInertia(rb.Mass);
                    break;
                case RigidBodyShape.Capsule:
                    var capsule = new Capsule(rb.Size.X, rb.Size.Y);
                    shapeIndex = _simulation.Shapes.Add(capsule);
                    inertia = capsule.ComputeInertia(rb.Mass);
                    break;
                case RigidBodyShape.Box:
                    var box = new Box(rb.Size.X, rb.Size.Y, rb.Size.Z);
                    shapeIndex = _simulation.Shapes.Add(box);
                    inertia = box.ComputeInertia(rb.Mass);
                    break;
                default:
                    continue;
            }

            var pose = new RigidPose(rb.Position,
                Quaternion.CreateFromYawPitchRoll(rb.Rotation.Y, rb.Rotation.X, rb.Rotation.Z));
            var collidable = new CollidableDescription(shapeIndex, 0.1f, rb.Friction, float.MaxValue, rb.Restitution);

            BodyDescription bodyDesc;
            if (rb.Mode == 0)
            {
                bodyDesc = BodyDescription.CreateKinematic(pose, collidable, new BodyActivityDescription());
            }
            else
            {
                bodyDesc = BodyDescription.CreateDynamic(pose, inertia, collidable, new BodyActivityDescription());
            }

            bodyDesc.LinearDamping = rb.LinearDamping;
            bodyDesc.AngularDamping = rb.AngularDamping;

            var handle = _simulation.Bodies.Add(bodyDesc);
            _bodyBoneMap[handle] = rb.BoneIndex;
        }
    }

    private struct SimpleNarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        public void Initialize(Simulation simulation) { }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) => true;

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
            where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial = new PairMaterialProperties(1f, 1f, new SpringSettings(30f, 1f));
            return true;
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;

        public void Dispose() { }
    }

    private struct SimplePoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        public Vector3 Gravity;
        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
        public bool AllowSubstepsForUnconstrainedBodies => false;
        public bool IntegrateVelocityForKinematics => false;

        public SimplePoseIntegratorCallbacks(Vector3 gravity)
        {
            Gravity = gravity;
        }

        public void Initialize(Simulation simulation) { }

        public void PrepareForIntegration(float dt) { }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
            BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            Vector3Wide.Broadcast(Gravity, out var g);
            velocity.Linear += g * dt;
        }
    }
}
