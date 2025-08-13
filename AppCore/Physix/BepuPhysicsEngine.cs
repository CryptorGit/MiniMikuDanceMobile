using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using MiniMikuDance.Data;
using MiniMikuDance.Import;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MiniMikuDance.Physix;

public sealed class BepuPhysicsEngine : IPhysicsEngine, IDisposable
{
    private static readonly BufferPool SharedBufferPool = new();
    private Simulation _simulation = null!;
    private MmdModel? _model;
    private BodyHandle[] _bodyHandles = Array.Empty<BodyHandle>();
    private CollidableProperty<PmxFilter> _collisionFilters = null!;

    public void Setup(MmdModel model)
    {
        _model = model;
        _simulation?.Dispose();
        _collisionFilters?.Dispose();
        _collisionFilters = new CollidableProperty<PmxFilter>();
        _simulation = Simulation.Create(SharedBufferPool,
            new NarrowPhaseCallbacks(model.RigidBodies, _collisionFilters),
            new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0), model.RigidBodies.ToArray()),
            new SolveDescription(8, 1));

        _bodyHandles = new BodyHandle[model.RigidBodies.Count];
        for (int i = 0; i < model.RigidBodies.Count; i++)
        {
            var rb = model.RigidBodies[i];
            RigidPose pose;
            if (rb.BoneIndex >= 0 && rb.BoneIndex < model.Bones.Count)
            {
                var bone = model.Bones[rb.BoneIndex];
                var orientation = bone.Rotation * rb.Rotation;
                var position = bone.Translation + Vector3.Transform(rb.Position, bone.Rotation);
                pose = new RigidPose(position, orientation);
            }
            else
            {
                pose = new RigidPose(rb.Position, rb.Rotation);
            }

            BodyDescription description;
            var activity = new BodyActivityDescription(0.01f);
            switch (rb.Shape)
            {
                case RigidBodyShape.Box:
                    var box = new Box(rb.Size.X, rb.Size.Y, rb.Size.Z);
                    var boxIndex = _simulation.Shapes.Add(box);
                    if (rb.Mass <= 0)
                    {
                        description = BodyDescription.CreateKinematic(pose,
                            new CollidableDescription(boxIndex, 0.1f), activity);
                    }
                    else
                    {
                        var boxInertia = box.ComputeInertia(rb.Mass);
                        description = BodyDescription.CreateDynamic(pose, boxInertia,
                            new CollidableDescription(boxIndex, 0.1f), activity);
                    }
                    break;
                case RigidBodyShape.Capsule:
                    var capsule = new Capsule(rb.Size.X, rb.Size.Y);
                    var capIndex = _simulation.Shapes.Add(capsule);
                    if (rb.Mass <= 0)
                    {
                        description = BodyDescription.CreateKinematic(pose,
                            new CollidableDescription(capIndex, 0.1f), activity);
                    }
                    else
                    {
                        var capInertia = capsule.ComputeInertia(rb.Mass);
                        description = BodyDescription.CreateDynamic(pose, capInertia,
                            new CollidableDescription(capIndex, 0.1f), activity);
                    }
                    break;
                default:
                    var sphere = new Sphere(rb.Size.X);
                    var sphereIndex = _simulation.Shapes.Add(sphere);
                    if (rb.Mass <= 0)
                    {
                        description = BodyDescription.CreateKinematic(pose,
                            new CollidableDescription(sphereIndex, 0.1f), activity);
                    }
                    else
                    {
                        var sphereInertia = sphere.ComputeInertia(rb.Mass);
                        description = BodyDescription.CreateDynamic(pose, sphereInertia,
                            new CollidableDescription(sphereIndex, 0.1f), activity);
                    }
                    break;
            }
            var handle = _simulation.Bodies.Add(description);
            _bodyHandles[i] = handle;
            ref var filter = ref _collisionFilters.Allocate(handle);
            filter = new PmxFilter { Group = rb.CollisionGroup, Mask = rb.CollisionMask };
        }

        foreach (var joint in model.Joints)
        {
            if (joint.RigidBodyA < 0 || joint.RigidBodyB < 0 ||
                joint.RigidBodyA >= _bodyHandles.Length || joint.RigidBodyB >= _bodyHandles.Length)
                continue;
            var handleA = _bodyHandles[joint.RigidBodyA];
            var handleB = _bodyHandles[joint.RigidBodyB];
            var bodyA = _simulation.Bodies.GetBodyReference(handleA);
            var bodyB = _simulation.Bodies.GetBodyReference(handleB);
            var poseA = bodyA.Pose;
            var poseB = bodyB.Pose;
            var anchorPosition = joint.Position;
            var offsetA = Vector3.Transform(anchorPosition - poseA.Position, Quaternion.Conjugate(poseA.Orientation));
            var offsetB = Vector3.Transform(anchorPosition - poseB.Position, Quaternion.Conjugate(poseB.Orientation));
            var orientationA = Quaternion.Concatenate(Quaternion.Conjugate(poseA.Orientation), joint.Rotation);
            var orientationB = Quaternion.Concatenate(Quaternion.Conjugate(poseB.Orientation), joint.Rotation);
            var socket = new BallSocket
            {
                LocalOffsetA = offsetA,
                LocalOffsetB = offsetB,
                SpringSettings = new SpringSettings(30f, 1f)
            };
            _simulation.Solver.Add(handleA, handleB, socket);

            Vector3[] axes = { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };
            for (int axis = 0; axis < 3; axis++)
            {
                float min = joint.LinearLowerLimit[axis];
                float max = joint.LinearUpperLimit[axis];
                if (min != 0f || max != 0f)
                {
                    var limit = new LinearAxisLimit
                    {
                        LocalOffsetA = offsetA,
                        LocalOffsetB = offsetB,
                        LocalAxis = Vector3.Transform(axes[axis], orientationA),
                        MinimumOffset = min,
                        MaximumOffset = max,
                        SpringSettings = new SpringSettings(MathF.Max(1e-3f, joint.LinearSpring[axis]), 1f)
                    };
                    _simulation.Solver.Add(handleA, handleB, limit);
                }
            }

            var angLower = joint.AngularLowerLimit;
            var angUpper = joint.AngularUpperLimit;
            var angSpring = joint.AngularSpring;
            if (angLower != Vector3.Zero || angUpper != Vector3.Zero || angSpring != Vector3.Zero)
            {
                if (angLower.X != 0f || angUpper.X != 0f)
                {
                    var twist = new TwistLimit
                    {
                        LocalBasisA = orientationA,
                        LocalBasisB = orientationB,
                        MinimumAngle = angLower.X,
                        MaximumAngle = angUpper.X,
                        SpringSettings = new SpringSettings(MathF.Max(1e-3f, angSpring.X), 1f)
                    };
                    _simulation.Solver.Add(handleA, handleB, twist);
                }
                if (angSpring.X != 0f)
                {
                    var servo = new TwistServo
                    {
                        LocalBasisA = orientationA,
                        LocalBasisB = orientationB,
                        TargetAngle = 0f,
                        SpringSettings = new SpringSettings(MathF.Max(1e-3f, angSpring.X), 1f),
                        ServoSettings = ServoSettings.Default
                    };
                    _simulation.Solver.Add(handleA, handleB, servo);
                }

                if (angLower.Y != 0f || angUpper.Y != 0f)
                {
                    var axisA = Vector3.Transform(Vector3.UnitY, orientationA);
                    var axisB = Vector3.Transform(Vector3.UnitY, orientationB);
                    var swingY = new SwingLimit
                    {
                        AxisLocalA = axisA,
                        AxisLocalB = axisB,
                        MaximumSwingAngle = MathF.Max(MathF.Abs(angLower.Y), MathF.Abs(angUpper.Y)),
                        SpringSettings = new SpringSettings(MathF.Max(1e-3f, angSpring.Y), 1f)
                    };
                    _simulation.Solver.Add(handleA, handleB, swingY);
                }

                if (angLower.Z != 0f || angUpper.Z != 0f)
                {
                    var axisA = Vector3.Transform(Vector3.UnitZ, orientationA);
                    var axisB = Vector3.Transform(Vector3.UnitZ, orientationB);
                    var swingZ = new SwingLimit
                    {
                        AxisLocalA = axisA,
                        AxisLocalB = axisB,
                        MaximumSwingAngle = MathF.Max(MathF.Abs(angLower.Z), MathF.Abs(angUpper.Z)),
                        SpringSettings = new SpringSettings(MathF.Max(1e-3f, angSpring.Z), 1f)
                    };
                    _simulation.Solver.Add(handleA, handleB, swingZ);
                }
            }
        }
    }

    public void Step(float deltaTime)
    {
        PreStep();
        const float step = 1f / 60f;
        var substepCount = Math.Max(1, (int)MathF.Ceiling(deltaTime / step));
        var substepDt = deltaTime / substepCount;
        for (var i = 0; i < substepCount; i++)
        {
            _simulation.Timestep(substepDt);
        }
        PostStep();
    }

    private void PreStep()
    {
        if (_model == null) return;
        for (int i = 0; i < _model.RigidBodies.Count; i++)
        {
            var rb = _model.RigidBodies[i];
            if (rb.Mass > 0 || rb.BoneIndex < 0 || rb.BoneIndex >= _model.Bones.Count)
                continue;
            var body = _simulation.Bodies.GetBodyReference(_bodyHandles[i]);
            var bone = _model.Bones[rb.BoneIndex];
            body.Pose.Orientation = bone.Rotation * rb.Rotation;
            body.Pose.Position = bone.Translation + Vector3.Transform(rb.Position, bone.Rotation);
            body.Velocity.Linear = Vector3.Zero;
            body.Velocity.Angular = Vector3.Zero;
        }
    }

    private void PostStep()
    {
        if (_model == null) return;
        for (int i = 0; i < _model.RigidBodies.Count; i++)
        {
            var rb = _model.RigidBodies[i];
            if (rb.Mass <= 0 || rb.BoneIndex < 0 || rb.BoneIndex >= _model.Bones.Count)
                continue;
            var body = _simulation.Bodies.GetBodyReference(_bodyHandles[i]);
            var bone = _model.Bones[rb.BoneIndex];
            var invRot = Quaternion.Inverse(rb.Rotation);
            bone.Rotation = body.Pose.Orientation * invRot;
            bone.Rotation = Quaternion.Normalize(bone.Rotation);
            bone.Translation = body.Pose.Position - Vector3.Transform(rb.Position, bone.Rotation);
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
        _collisionFilters?.Dispose();
    }

    private struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        private Simulation _simulation;
        private readonly IReadOnlyList<RigidBodyData> _rigidBodies;
        public CollidableProperty<PmxFilter> Filters;

        public NarrowPhaseCallbacks(IReadOnlyList<RigidBodyData> rigidBodies, CollidableProperty<PmxFilter> filters)
        {
            _rigidBodies = rigidBodies;
            Filters = filters;
            _simulation = null!;
        }

        public void Initialize(Simulation simulation)
        {
            _simulation = simulation;
            Filters.Initialize(simulation);
        }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
            => (a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic) &&
               PmxFilter.Allow(Filters[a], Filters[b]);

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            int indexA = _simulation.Bodies.HandleToLocation[pair.A.BodyHandle.Value].Index;
            int indexB = _simulation.Bodies.HandleToLocation[pair.B.BodyHandle.Value].Index;
            var rbA = _rigidBodies[indexA];
            var rbB = _rigidBodies[indexB];
            pairMaterial.FrictionCoefficient = (rbA.Friction + rbB.Friction) * 0.5f;
            pairMaterial.MaximumRecoveryVelocity = MathF.Max(rbA.Restitution, rbB.Restitution);
            pairMaterial.SpringSettings = new SpringSettings(30f, 1f);
            return true;
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;
        public void Dispose() { }
    }

    private struct PmxFilter
    {
        public byte Group;
        public ushort Mask;

        public static bool Allow(in PmxFilter a, in PmxFilter b)
        {
            return (a.Mask & (1 << b.Group)) == 0 && (b.Mask & (1 << a.Group)) == 0;
        }
    }

    private struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        private Vector3 _gravity;
        private Vector3Wide _gravityDt;
        private RigidBodyData[] _rigidBodies;
        public PoseIntegratorCallbacks(Vector3 gravity, RigidBodyData[] rigidBodies) : this()
        {
            _gravity = gravity;
            _rigidBodies = rigidBodies;
        }
        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
        public bool AllowSubstepsForUnconstrainedBodies => false;
        public bool IntegrateVelocityForKinematics => false;
        public void Initialize(Simulation simulation) { }
        public void PrepareForIntegration(float dt) => _gravityDt = Vector3Wide.Broadcast(_gravity * dt);
        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            velocity.Linear += _gravityDt;
            for (int lane = 0; lane < Vector<float>.Count; lane++)
            {
                int index = GatherScatter.Get(ref bodyIndices, lane);
                if (index < 0 || index >= _rigidBodies.Length) continue;
                var rb = _rigidBodies[index];
                float dl = 1f - rb.TranslationDamping * GatherScatter.Get(ref dt, lane);
                float da = 1f - rb.RotationDamping * GatherScatter.Get(ref dt, lane);
                GatherScatter.Get(ref velocity.Linear.X, lane) *= dl;
                GatherScatter.Get(ref velocity.Linear.Y, lane) *= dl;
                GatherScatter.Get(ref velocity.Linear.Z, lane) *= dl;
                GatherScatter.Get(ref velocity.Angular.X, lane) *= da;
                GatherScatter.Get(ref velocity.Angular.Y, lane) *= da;
                GatherScatter.Get(ref velocity.Angular.Z, lane) *= da;
            }
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
