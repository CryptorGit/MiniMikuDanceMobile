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
using MiniMikuDance.Physics.Cloth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiniMikuDance.Physics;

public sealed class BepuPhysicsWorld : IPhysicsWorld
{
    private BufferPool? _bufferPool;
    private Simulation? _simulation;
    private readonly Dictionary<BodyHandle, (int Bone, int Mode)> _bodyBoneMap = new();
    private readonly List<BodyHandle> _rigidBodyHandles = new();
    private readonly Dictionary<BodyHandle, Material> _materialMap = new();
    private readonly Dictionary<BodyHandle, SubgroupCollisionFilter> _bodyFilterMap = new();
    private readonly ClothSimulator _cloth = new();
    private PhysicsConfig _config;
    private float _modelScale = 1f;
    private float _massScale = 1f;
    private readonly ILogger<BepuPhysicsWorld> _logger;

    public float BoneBlendFactor { get; set; } = 0.5f;

    public BepuPhysicsWorld(ILogger<BepuPhysicsWorld>? logger = null)
    {
        _logger = logger ?? NullLogger<BepuPhysicsWorld>.Instance;
    }

    public void Initialize(PhysicsConfig config, float modelScale)
    {
        _modelScale = modelScale;
        _massScale = modelScale * modelScale * modelScale;
        var scaledGravity = config.Gravity * modelScale;
        var substepCount = config.SubstepCount;
        if (substepCount <= 0)
        {
            _logger.LogWarning("SubstepCount が 0 以下のため、1 に補正しました。");
            substepCount = 1;
        }
        var solverIterationCount = config.SolverIterationCount;
        if (solverIterationCount < 1)
        {
            _logger.LogWarning("SolverIterationCount が 0 以下のため、1 に補正しました。");
            solverIterationCount = 1;
        }
        _config = new PhysicsConfig(scaledGravity, solverIterationCount, substepCount, config.Damping, config.BoneBlendFactor);
        BoneBlendFactor = config.BoneBlendFactor;
        _bufferPool = new BufferPool();
        _simulation = Simulation.Create(_bufferPool,
            new SubgroupFilteredCallbacks(_materialMap, _bodyFilterMap),
            new SimplePoseIntegratorCallbacks(scaledGravity, _config.Damping, _config.Damping),
            new SolveDescription(solverIterationCount, substepCount));
        _cloth.Gravity = scaledGravity;
        _cloth.Damping = config.Damping;
    }

    public void Step(float dt)
    {
        _cloth.Gravity = _config.Gravity;
        _cloth.Damping = _config.Damping;
        _simulation?.Timestep(dt);
        _cloth.Step(dt);
    }

    /// <inheritdoc/>
    public void SyncFromBones(Scene scene)
    {
        if (_simulation is null)
            return;

        foreach (var pair in _bodyBoneMap)
        {
            var handle = pair.Key;
            var info = pair.Value;
            if (info.Bone < 0 || info.Bone >= scene.Bones.Count)
                continue;
            if (info.Mode == 1)
                continue;

            var body = _simulation.Bodies.GetBodyReference(handle);
            var bone = scene.Bones[info.Bone];
            body.Pose.Position = bone.Translation;
            body.Pose.Orientation = bone.Rotation;
            body.Velocity.Linear = Vector3.Zero;
            body.Velocity.Angular = Vector3.Zero;
        }
    }

    public void SyncToBones(Scene scene)
    {
        if (_simulation is not null)
        {
            var poseMap = new Dictionary<int, (Vector3 Pos, Quaternion Rot)>();
            foreach (var pair in _bodyBoneMap)
            {
                var body = _simulation.Bodies.GetBodyReference(pair.Key);
                poseMap[pair.Value.Bone] = (body.Pose.Position, body.Pose.Orientation);
            }

            var cache = new Dictionary<int, Matrix4x4>();
            foreach (var pair in _bodyBoneMap)
            {
                var info = pair.Value;
                if (info.Bone < 0 || info.Bone >= scene.Bones.Count)
                    continue;
                if (info.Mode == 0)
                    continue;
                var bone = scene.Bones[info.Bone];
                var pose = poseMap[info.Bone];
                Quaternion localRot;
                if (bone.Parent >= 0)
                {
                    var parentWorld = GetWorldMatrix(scene, bone.Parent, poseMap, cache);
                    Matrix4x4.Invert(parentWorld, out var invParent);
                    var world = Matrix4x4.CreateFromQuaternion(pose.Rot) * Matrix4x4.CreateTranslation(pose.Pos);
                    var local = world * invParent;
                    Matrix4x4.Decompose(local, out _, out localRot, out _);
                }
                else
                {
                    localRot = pose.Rot;
                }

                if (info.Mode == 2)
                {
                    bone.Rotation = Quaternion.Slerp(bone.Rotation, localRot, BoneBlendFactor);
                }
                else
                {
                    bone.Rotation = localRot;
                }
            }
        }

        // Soft body (rope, etc.) simulation results
        _cloth.SyncToBones(scene);
    }

    private static Matrix4x4 GetWorldMatrix(Scene scene, int index,
        Dictionary<int, (Vector3 Pos, Quaternion Rot)> poses, Dictionary<int, Matrix4x4> cache)
    {
        if (cache.TryGetValue(index, out var mat))
            return mat;

        Matrix4x4 local;
        if (poses.TryGetValue(index, out var pose))
            local = Matrix4x4.CreateFromQuaternion(pose.Rot) * Matrix4x4.CreateTranslation(pose.Pos);
        else
        {
            var bone = scene.Bones[index];
            local = Matrix4x4.CreateFromQuaternion(bone.Rotation) * Matrix4x4.CreateTranslation(bone.Translation);
        }

        var boneData = scene.Bones[index];
        if (boneData.Parent >= 0)
            mat = local * GetWorldMatrix(scene, boneData.Parent, poses, cache);
        else
            mat = local;

        cache[index] = mat;
        return mat;
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
            var mass = rb.Mass * _massScale;
            TypedIndex shapeIndex;
            BodyInertia inertia = default;
            switch (rb.Shape)
            {
                case RigidBodyShape.Sphere:
                    var sphere = new Sphere(rb.Size.X);
                    shapeIndex = _simulation.Shapes.Add(sphere);
                    inertia = sphere.ComputeInertia(mass);
                    break;
                case RigidBodyShape.Capsule:
                    var capsule = new Capsule(rb.Size.X, rb.Size.Y);
                    shapeIndex = _simulation.Shapes.Add(capsule);
                    inertia = capsule.ComputeInertia(mass);
                    break;
                case RigidBodyShape.Box:
                    var box = new Box(rb.Size.X, rb.Size.Y, rb.Size.Z);
                    shapeIndex = _simulation.Shapes.Add(box);
                    inertia = box.ComputeInertia(mass);
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

    public void LoadSoftBodies(ModelData model)
    {
        _cloth.Nodes.Clear();
        _cloth.Springs.Clear();
        _cloth.BoneMap.Clear();

        foreach (var sb in model.SoftBodies)
        {
            switch (sb.Shape)
            {
                case SoftBodyShape.Rope:
                case SoftBodyShape.TriMesh:
                case SoftBodyShape.Cloth:
                    BuildSoftBodyFromBones(model, sb);
                    break;
            }
        }
    }

    private void BuildSoftBodyFromBones(ModelData model, SoftBodyData sb)
    {
        var rootIndex = model.Bones.FindIndex(b => b.Name == sb.Name || b.NameEnglish == sb.NameEnglish);
        if (rootIndex < 0)
            return;

        var queue = new Queue<(int Bone, int ParentNode)>();
        queue.Enqueue((rootIndex, -1));

        while (queue.Count > 0)
        {
            var (boneIndex, parentNode) = queue.Dequeue();
            var bone = model.Bones[boneIndex];

            var nodeIndex = _cloth.Nodes.Count;
            var mass = sb.NodeMass > 0f ? sb.NodeMass * _massScale : _massScale;
            var invMass = parentNode < 0 ? 0f : 1f / mass;
            _cloth.Nodes.Add(new Node { Position = bone.Translation, Velocity = Vector3.Zero, InverseMass = invMass });
            _cloth.BoneMap.Add(boneIndex);

            if (parentNode >= 0)
            {
                var prevPos = _cloth.Nodes[parentNode].Position;
                var rest = Vector3.Distance(prevPos, bone.Translation);
                _cloth.Springs.Add(new Spring
                {
                    NodeA = parentNode,
                    NodeB = nodeIndex,
                    RestLength = rest,
                    Stiffness = sb.SpringStiffness,
                    Damping = sb.SpringDamping
                });
            }

            var children = model.Bones.FindAll(b => b.Parent == boneIndex);
            foreach (var child in children)
            {
                var childIndex = model.Bones.IndexOf(child);
                if (childIndex >= 0)
                    queue.Enqueue((childIndex, nodeIndex));
            }
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

            var averageSpring = (jd.SpringPosition.X + jd.SpringPosition.Y + jd.SpringPosition.Z) / 3f;
            var ball = new BallSocket
            {
                LocalOffsetA = localA.Position,
                LocalOffsetB = localB.Position,
                SpringSettings = ToSpringSettings(averageSpring)
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
            SpringSettings = ToSpringSettings(spring)
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
            SpringSettings = ToSpringSettings(spring)
        };
        _simulation!.Solver.Add(a, b, limit);
    }

    private static SpringSettings ToSpringSettings(float pmxSpring)
    {
        var frequency = MathF.Sqrt(MathF.Max(pmxSpring, 0f));
        frequency = Math.Clamp(frequency, 0.0001f, 60f);
        const float dampingRatio = 1f;
        return new SpringSettings(frequency, dampingRatio);
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
        public float LinearDamping;
        public float AngularDamping;
        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
        public bool AllowSubstepsForUnconstrainedBodies => false;
        public bool IntegrateVelocityForKinematics => false;

        public SimplePoseIntegratorCallbacks(Vector3 gravity, float linearDamping, float angularDamping)
        {
            Gravity = gravity;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
        }

        public void Initialize(Simulation simulation) { }

        public void PrepareForIntegration(float dt) { }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
            BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            Vector3Wide.Broadcast(Gravity, out var g);
            velocity.Linear += g * dt;
            var linear = new Vector<float>(LinearDamping);
            var angular = new Vector<float>(AngularDamping);
            Vector3Wide.Scale(velocity.Linear, linear, out velocity.Linear);
            Vector3Wide.Scale(velocity.Angular, angular, out velocity.Angular);
        }
    }
}
