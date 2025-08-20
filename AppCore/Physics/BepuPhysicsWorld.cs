using System;
using System.Numerics;
using System.Collections.Generic;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using MiniMikuDance.Import;
using MiniMikuDance.App;

namespace MiniMikuDance.Physics;

public sealed class BepuPhysicsWorld : IPhysicsWorld
{
    private BufferPool? _bufferPool;
    private Simulation? _simulation;
    private readonly Dictionary<BodyHandle, (int Bone, int Mode)> _bodyBoneMap = new();
    private readonly List<BodyHandle> _rigidBodyHandles = new();
    private readonly Dictionary<BodyHandle, Material> _materialMap = new();
    private readonly Dictionary<BodyHandle, SubgroupCollisionFilter> _bodyFilterMap = new();

    public void Initialize()
    {
        _bufferPool = new BufferPool();
        _simulation = Simulation.Create(_bufferPool,
            new SubgroupFilteredCallbacks(_materialMap, _bodyFilterMap),
            new SimplePoseIntegratorCallbacks(new Vector3(0, -9.81f, 0)),
            new SolveDescription(8, 1));
    }

    public void Step(float dt)
    {
        _simulation?.Timestep(dt);
    }

    public void SyncToBones(Scene scene)
    {
        if (_simulation is null) return;
        foreach (var pair in _bodyBoneMap)
        {
            var handle = pair.Key;
            var info = pair.Value;
            if (info.Bone < 0 || info.Bone >= scene.Bones.Count)
                continue;
            if (info.Mode == 0)
                continue;
            var body = _simulation.Bodies.GetBodyReference(handle);
            var pose = body.Pose;
            var bone = scene.Bones[info.Bone];
            bone.Translation = pose.Position;
            bone.Rotation = pose.Orientation;
        }
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
        _rigidBodyHandles.Clear();
        _materialMap.Clear();
        _bodyFilterMap.Clear();
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
            var filter = new SubgroupCollisionFilter((uint)rb.Group, (uint)rb.Mask);
            var collidable = new CollidableDescription(shapeIndex, 0.1f);

            BodyDescription bodyDesc;
            if (rb.Mode == 0)
            {
                bodyDesc = BodyDescription.CreateKinematic(pose, collidable, new BodyActivityDescription());
            }
            else
            {
                bodyDesc = BodyDescription.CreateDynamic(pose, inertia, collidable, new BodyActivityDescription());
            }


            var handle = _simulation.Bodies.Add(bodyDesc);
            _rigidBodyHandles.Add(handle);
            _bodyBoneMap[handle] = (rb.BoneIndex, rb.Mode);
            _materialMap[handle] = new Material(rb.Restitution, rb.Friction);
            _bodyFilterMap[handle] = filter;
        }
    }

    public void LoadJoints(ModelData model)
    {
        if (_simulation is null) return;
        for (int i = 0; i < model.Joints.Count; i++)
        {
            var jd = model.Joints[i];
            if (jd.RigidBodyA < 0 || jd.RigidBodyB < 0) continue;
            if (jd.RigidBodyA >= _rigidBodyHandles.Count || jd.RigidBodyB >= _rigidBodyHandles.Count) continue;

            var handleA = _rigidBodyHandles[jd.RigidBodyA];
            var handleB = _rigidBodyHandles[jd.RigidBodyB];
            var rbA = model.RigidBodies[jd.RigidBodyA];
            var rbB = model.RigidBodies[jd.RigidBodyB];

            ComputeJointLocalPoses(jd, rbA, rbB, out var localA, out var localB);

            var ball = new BallSocket
            {
                LocalOffsetA = localA.Position,
                LocalOffsetB = localB.Position,
                SpringSettings = new SpringSettings(30f, 1f)
            };
            _simulation.Solver.Add(handleA, handleB, ball);

            AddLinearLimit(handleA, handleB, localA.Position, localB.Position, localA.Orientation,
                new Vector3(1, 0, 0), jd.PositionMin.X, jd.PositionMax.X, jd.SpringPosition.X);
            AddLinearLimit(handleA, handleB, localA.Position, localB.Position, localA.Orientation,
                new Vector3(0, 1, 0), jd.PositionMin.Y, jd.PositionMax.Y, jd.SpringPosition.Y);
            AddLinearLimit(handleA, handleB, localA.Position, localB.Position, localA.Orientation,
                new Vector3(0, 0, 1), jd.PositionMin.Z, jd.PositionMax.Z, jd.SpringPosition.Z);

            AddTwistLimit(handleA, handleB, localA.Orientation, localB.Orientation,
                Vector3.UnitX, jd.RotationMin.X, jd.RotationMax.X, jd.SpringRotation.X);
            AddTwistLimit(handleA, handleB, localA.Orientation, localB.Orientation,
                Vector3.UnitY, jd.RotationMin.Y, jd.RotationMax.Y, jd.SpringRotation.Y);
            AddTwistLimit(handleA, handleB, localA.Orientation, localB.Orientation,
                Vector3.UnitZ, jd.RotationMin.Z, jd.RotationMax.Z, jd.SpringRotation.Z);
        }
    }

    private void AddLinearLimit(BodyHandle a, BodyHandle b, Vector3 localOffsetA, Vector3 localOffsetB,
        Quaternion localOrientationA, Vector3 axis, float min, float max, float spring)
    {
        if (min == 0f && max == 0f && spring == 0f) return;
        var localAxis = Vector3.Transform(axis, Quaternion.Conjugate(localOrientationA));
        var limit = new LinearAxisLimit
        {
            LocalOffsetA = localOffsetA,
            LocalOffsetB = localOffsetB,
            LocalAxis = localAxis,
            MinimumOffset = min,
            MaximumOffset = max,
            SpringSettings = new SpringSettings(MathF.Max(spring, 0.0001f), 1f)
        };
        _simulation!.Solver.Add(a, b, limit);
    }

    private void AddTwistLimit(BodyHandle a, BodyHandle b, Quaternion localOrientationA, Quaternion localOrientationB,
        Vector3 axis, float min, float max, float spring)
    {
        if (min == 0f && max == 0f && spring == 0f) return;
        var basis = CreateBasisFromAxis(axis);
        var limit = new TwistLimit
        {
            LocalBasisA = Quaternion.Concatenate(basis, localOrientationA),
            LocalBasisB = Quaternion.Concatenate(basis, localOrientationB),
            MinimumAngle = min,
            MaximumAngle = max,
            SpringSettings = new SpringSettings(MathF.Max(spring, 0.0001f), 1f)
        };
        _simulation!.Solver.Add(a, b, limit);
    }

    private static void ComputeJointLocalPoses(JointData joint, RigidBodyData a, RigidBodyData b,
        out RigidPose localA, out RigidPose localB)
    {
        var jointOrientation = Quaternion.CreateFromYawPitchRoll(joint.Rotation.Y, joint.Rotation.X, joint.Rotation.Z);
        localA = ToLocalPose(joint.Position, jointOrientation, a);
        localB = ToLocalPose(joint.Position, jointOrientation, b);
    }

    private static RigidPose ToLocalPose(Vector3 jointPos, Quaternion jointOrientation, RigidBodyData rb)
    {
        var bodyOrientation = Quaternion.CreateFromYawPitchRoll(rb.Rotation.Y, rb.Rotation.X, rb.Rotation.Z);
        var invBody = Quaternion.Conjugate(bodyOrientation);
        var localPos = Vector3.Transform(jointPos - rb.Position, invBody);
        var localRot = Quaternion.Concatenate(invBody, jointOrientation);
        return new RigidPose(localPos, localRot);
    }

    private static Quaternion CreateBasisFromAxis(Vector3 axis)
    {
        axis = Vector3.Normalize(axis);
        var dot = Vector3.Dot(Vector3.UnitZ, axis);
        if (dot > 0.9999f) return Quaternion.Identity;
        if (dot < -0.9999f) return Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
        var rotAxis = Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, axis));
        var angle = MathF.Acos(dot);
        return Quaternion.CreateFromAxisAngle(rotAxis, angle);
    }

    private struct SubgroupFilteredCallbacks : INarrowPhaseCallbacks
    {
        private readonly Dictionary<BodyHandle, Material> _materials;
        private readonly Dictionary<BodyHandle, SubgroupCollisionFilter> _filters;

        public SubgroupFilteredCallbacks(Dictionary<BodyHandle, Material> materials,
            Dictionary<BodyHandle, SubgroupCollisionFilter> filters)
        {
            _materials = materials;
            _filters = filters;
        }

        public void Initialize(Simulation simulation) { }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            _filters.TryGetValue(a.BodyHandle, out var filterA);
            _filters.TryGetValue(b.BodyHandle, out var filterB);
            return SubgroupCollisionFilter.AllowCollision(filterA, filterB);
        }

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
            out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            _materials.TryGetValue(pair.A.BodyHandle, out var matA);
            _materials.TryGetValue(pair.B.BodyHandle, out var matB);
            var friction = (matA.Friction + matB.Friction) * 0.5f;
            var restitution = (matA.Restitution + matB.Restitution) * 0.5f;
            pairMaterial = new PairMaterialProperties(friction, restitution, new SpringSettings(30f, 1f));
            return true;
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
            ref ConvexContactManifold manifold) => true;

        public void Dispose() { }
    }

    private struct Material
    {
        public float Restitution;
        public float Friction;

        public Material(float restitution, float friction)
        {
            Restitution = restitution;
            Friction = friction;
        }
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
