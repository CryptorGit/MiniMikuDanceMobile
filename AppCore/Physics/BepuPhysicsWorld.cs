using System;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuPhysics.CollisionDetection;
using BepuPhysics.CollisionDetection.CollisionTasks;
using BepuUtilities;
using BepuUtilities.Memory;
using MiniMikuDance.Import;
using MiniMikuDance.App;
using MiniMikuDance.Physics.Cloth;
using ClothNode = MiniMikuDance.Physics.Cloth.Node;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiniMikuDance.Physics;

public sealed class BepuPhysicsWorld : IPhysicsWorld
{
    private BufferPool? _bufferPool;
    private Simulation? _simulation;
    // ThreadDispatcher は IThreadDispatcher を実装する
    private ThreadDispatcher? _threadDispatcher;
    private readonly Dictionary<BodyHandle, (int Bone, RigidBodyMode Mode)> _bodyBoneMap = new(); // Mode: FollowBone=0, Physics=1, PhysicsWithBoneAlignment=2
    private readonly List<BodyHandle> _rigidBodyHandles = new();
    private readonly List<TypedIndex> _shapeIndices = new();
    private readonly Dictionary<BodyHandle, Material> _materialMap = new();
    private readonly Dictionary<BodyHandle, SubgroupCollisionFilter> _bodyFilterMap = new();
    private readonly Dictionary<BodyHandle, (float Linear, float Angular)> _dampingMap = new();
    private readonly Dictionary<StaticHandle, Material> _staticMaterialMap = new();
    private readonly Dictionary<StaticHandle, SubgroupCollisionFilter> _staticFilterMap = new();
    private readonly ClothSimulator _cloth;
    private readonly Dictionary<int, (Vector3 Pos, Quaternion Rot)> _prevBonePoses = new();
    private readonly Dictionary<BodyHandle, BodyInertia> _originalInertiaMap = new();
    private readonly Dictionary<int, (Vector3 Pos, Quaternion Rot)> _poseCache = new();
    private readonly Dictionary<int, (Vector3 Pos, Quaternion Rot)> _initialCache = new();
    private readonly Dictionary<int, (Vector3 Pos, Quaternion Rot)> _poseMap = new();
    private readonly Dictionary<int, Matrix4x4> _matrixCache = new();
    private readonly Dictionary<int, IkLink> _ikLinkMap = new();
    private bool _skipSimulation;
    private PhysicsConfig _config = new() { LockTranslation = false };
    private float _modelScale = 1f;
    private readonly ILogger _logger;
    private float _lastDt = 1f / 60f;
    private int _frameIndex;

    public float BoneBlendFactor { get; set; } = 0.5f;
    public bool LockTranslation
    {
        get => _config.LockTranslation;
        set
        {
            var wasLocked = _config.LockTranslation;
            _config.LockTranslation = value;
            _cloth.LockTranslation = value;
            if (!value && wasLocked && _simulation is not null)
            {
                foreach (var pair in _originalInertiaMap)
                {
                    var body = _simulation.Bodies.GetBodyReference(pair.Key);
                    body.LocalInertia = pair.Value;
                }
                _originalInertiaMap.Clear();
            }
        }
    }

    public BepuPhysicsWorld(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _cloth = new ClothSimulator(_logger);
    }

    /// <summary>
    /// 物理シミュレーションを初期化する。
    /// Dispose 後に再利用する場合は、本メソッドを再度呼び出すこと。
    /// バッファプール → スレッドディスパッチャ → Simulation の順に生成され、
    /// 依存関係が解決される。
    /// </summary>
    public void Initialize(PhysicsConfig config, float modelScale)
    {
        _simulation?.Dispose();
        _bufferPool?.Clear();
        _simulation = null;
        _bufferPool = null;

        _skipSimulation = false;
        _modelScale = modelScale;
        var gravity = config.Gravity;
        bool IsInvalid(float v) => float.IsNaN(v) || float.IsInfinity(v) || v < -1000f || v > 1000f;
        bool IsNearZero(float v) => MathF.Abs(v) < 1e-3f;
        if (IsInvalid(gravity.X) || IsInvalid(gravity.Y) || IsInvalid(gravity.Z) ||
            (IsNearZero(gravity.X) && IsNearZero(gravity.Y) && IsNearZero(gravity.Z)))
        {
            var corrected = new Vector3(0f, -9.81f, 0f);
            _logger.LogWarning("Invalid gravity passed: {Gravity}. Resetting to {Corrected}.", gravity, corrected);
            gravity = corrected;
        }
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

        var maxThreadCount = config.MaxThreadCount;
        if (maxThreadCount <= 0)
        {
            var defaultCount = Environment.ProcessorCount;
            _logger.LogWarning("MaxThreadCount が 0 以下のため、{Default} に補正しました。", defaultCount);
            maxThreadCount = defaultCount;
        }

        _config = new PhysicsConfig(gravity, solverIterationCount, substepCount, config.Damping, config.BoneBlendFactor, config.GroundHeight, config.Restitution, config.RestitutionRecoveryScale, config.Friction, config.LockTranslation, maxThreadCount);
        BoneBlendFactor = _config.BoneBlendFactor;
        _bufferPool = new BufferPool();
        _threadDispatcher?.Dispose();
        _threadDispatcher = null;
        var threads = Math.Clamp(Environment.ProcessorCount, 1, maxThreadCount);
        _threadDispatcher = new ThreadDispatcher(threads);
        _simulation = Simulation.Create(_bufferPool,
            new SubgroupFilteredCallbacks(_materialMap, _bodyFilterMap, _staticMaterialMap, _staticFilterMap),
            // Damping は 1 秒あたりの減衰率 (0～1)
            new SimplePoseIntegratorCallbacks(gravity, _config.Damping, _config.Damping, _dampingMap),
            new SolveDescription(solverIterationCount, substepCount));
        // Simulation 生成後に ConstraintRemover が確実に登録されているかチェック
        if (_simulation.NarrowPhase.ConstraintRemover == null)
        {
            _simulation.NarrowPhase.ConstraintRemover = new ConstraintRemover(_bufferPool, _simulation.Bodies, _simulation.Solver);
        }

        _staticMaterialMap.Clear();
        _staticFilterMap.Clear();

        // シミュレーション生成後に地面用の静的ボディを追加
        var groundShape = _simulation.Shapes.Add(new Box(1000f * _modelScale, 0.1f * _modelScale, 1000f * _modelScale));
        var groundPose = new RigidPose(new Vector3(0f, _config.GroundHeight - 0.05f * _modelScale, 0f));
        var groundDesc = new StaticDescription(
            groundPose.Position,
            groundPose.Orientation,
            groundShape,
            ContinuousDetection.Discrete);
        var groundHandle = _simulation.Statics.Add(groundDesc);
        var groundMaxRecovery = ComputeMaxRecoveryVelocity(_config.Restitution, _config.RestitutionRecoveryScale);
        _staticMaterialMap[groundHandle] = new Material(_config.Friction, groundMaxRecovery);
        _staticFilterMap[groundHandle] = new SubgroupCollisionFilter(uint.MaxValue, uint.MaxValue);

        _cloth.Gravity = gravity;
        _cloth.Damping = _config.Damping; // 1 秒基準のダンピング
        _cloth.GroundHeight = _config.GroundHeight;
        _cloth.Restitution = _config.Restitution;
        _cloth.Friction = _config.Friction;
        _cloth.LockTranslation = _config.LockTranslation;
        _cloth.Substeps = _config.SubstepCount;
    }

    public void Step(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0f)
        {
            _logger.LogWarning("dt が不正な値のため Step をスキップします: {Dt}", dt);
            return;
        }
        if (_skipSimulation)
        {
            return;
        }
        if (_simulation is null || _threadDispatcher is null)
        {
            _logger.LogWarning("Simulation が初期化されていないため Step をスキップします。");
            return;
        }
        _cloth.Gravity = _config.Gravity;
        _cloth.Damping = _config.Damping;
        _cloth.GroundHeight = _config.GroundHeight;
        _cloth.Restitution = _config.Restitution;
        _cloth.Friction = _config.Friction;
        _cloth.LockTranslation = _config.LockTranslation;
        _cloth.Substeps = _config.SubstepCount;
        _logger.LogDebug("Step start dt={Dt}, memory={Memory}", dt, GC.GetTotalMemory(false));
        try
        {
            _simulation.Timestep(dt, _threadDispatcher);
        }
        catch (AccessViolationException ave)
        {
            _logger.LogCritical(ave, "Access violation in Simulation.Timestep");
            AppendCrashLog("Simulation.Timestep access violation", ave);
            _skipSimulation = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Simulation.Timestep に失敗しました。");
            LogBodyAabbs(LogLevel.Error);
            LogCollidablePairs(LogLevel.Error);
            AppendCrashLog("Simulation.Timestep failed", ex);
            _skipSimulation = true;
        }
        if (_skipSimulation)
        {
            return;
        }
        var bodyCount = _simulation.Bodies.ActiveSet.Count;
        var leafCount = _simulation.BroadPhase.ActiveTree.LeafCount;
        Debug.Assert(!NeedsBroadPhaseRebuild(bodyCount, leafCount));
        if (NeedsBroadPhaseRebuild(bodyCount, leafCount))
        {
            _logger.LogWarning("剛体数({BodyCount})と BroadPhase 葉数({LeafCount}) の不一致", bodyCount, leafCount);
            RefitAndRefineBroadPhase();
            bodyCount = _simulation.Bodies.ActiveSet.Count;
            leafCount = _simulation.BroadPhase.ActiveTree.LeafCount;
            if (NeedsBroadPhaseRebuild(bodyCount, leafCount))
            {
                _logger.LogError("BroadPhase 再構築後も不一致: BodyCount={BodyCount}, LeafCount={LeafCount}", bodyCount, leafCount);
                _skipSimulation = true;
            }
        }
        if (_skipSimulation)
        {
            return;
        }
        try
        {
            _cloth.Step(dt);
        }
        catch (AccessViolationException ave)
        {
            _logger.LogCritical(ave, "Access violation in Cloth.Step");
            AppendCrashLog("Cloth.Step access violation", ave);
            _skipSimulation = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloth.Step failed");
            AppendCrashLog("Cloth.Step failed", ex);
            _skipSimulation = true;
        }
        if (_skipSimulation)
        {
            return;
        }
        _lastDt = dt;
        _logger.LogDebug("Step end memory={Memory}", GC.GetTotalMemory(false));
    }

    public Vector3 GetGravity()
    {
        return _config.Gravity;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// 剛体モードの挙動:
    /// 0 = ボーン追従 (キネマティック)、
    /// 1 = 物理のみ (同期なし)、
    /// 2 = 物理+ボーン (骨から速度を与え、結果をボーンへ反映)。
    /// </remarks>
    public void SyncFromBones(Scene scene)
    {
        if (_skipSimulation || _simulation is null)
            return;
        _poseCache.Clear();
        _initialCache.Clear();
        var mapSnapshotFrom = _bodyBoneMap.ToArray();
        foreach (var pair in mapSnapshotFrom)
        {
            var handle = pair.Key;
            var info = pair.Value;
            if (info.Bone < 0 || info.Bone >= scene.Bones.Count)
                continue;
            if (info.Mode == RigidBodyMode.Physics)
                continue;

            var body = _simulation.Bodies.GetBodyReference(handle);
            if (_config.LockTranslation)
            {
                var initPose = GetInitialWorldPose(scene, info.Bone, _initialCache);
                body.Pose.Position = initPose.Pos;
                body.Pose.Orientation = initPose.Rot;
                body.Velocity.Linear = Vector3.Zero;
                body.Velocity.Angular = Vector3.Zero;
                if (!_originalInertiaMap.ContainsKey(handle))
                    _originalInertiaMap[handle] = body.LocalInertia;
                body.LocalInertia = default;
                _prevBonePoses[info.Bone] = initPose;
                continue;
            }

            var pose = GetWorldPose(scene, info.Bone, _poseCache);
            body.Pose.Position = pose.Pos;
            body.Pose.Orientation = pose.Rot;
            UpdateBodyVelocity(info.Bone, pose.Pos, pose.Rot, ref body);
        }

        var nodeCount = Math.Min(_cloth.Nodes.Count, _cloth.BoneMap.Count);
        for (int i = 0; i < nodeCount; i++)
        {
            var node = _cloth.Nodes[i];
            if (node.InverseMass > 0f)
                continue;

            var boneIndex = _cloth.BoneMap[i];
            if (boneIndex < 0 || boneIndex >= scene.Bones.Count)
                continue;

            var pose = GetWorldPose(scene, boneIndex, _poseCache);
            var newPos = pose.Pos;
            node.Velocity = (newPos - node.Position) / _lastDt;
            node.Position = newPos;
            _cloth.Nodes[i] = node;
        }
    }

    private static (Vector3 Pos, Quaternion Rot) GetWorldPose(Scene scene, int index,
        Dictionary<int, (Vector3 Pos, Quaternion Rot)> cache)
    {
        if (cache.TryGetValue(index, out var pose))
            return pose;

        var bone = scene.Bones[index];
        if (bone.Parent >= 0)
        {
            var parent = GetWorldPose(scene, bone.Parent, cache);
            var rot = parent.Rot * bone.Rotation;
            var pos = Vector3.Transform(bone.Translation, parent.Rot) + parent.Pos;
            pose = (pos, rot);
        }
        else
        {
            pose = (bone.Translation, bone.Rotation);
        }

        cache[index] = pose;
        return pose;
    }

    private static (Vector3 Pos, Quaternion Rot) GetInitialWorldPose(Scene scene, int index,
        Dictionary<int, (Vector3 Pos, Quaternion Rot)> cache)
    {
        if (cache.TryGetValue(index, out var pose))
            return pose;

        var bone = scene.Bones[index];
        if (bone.Parent >= 0)
        {
            var parent = GetInitialWorldPose(scene, bone.Parent, cache);
            var rot = parent.Rot * bone.InitialRotation;
            var pos = Vector3.Transform(bone.InitialTranslation, parent.Rot) + parent.Pos;
            pose = (pos, rot);
        }
        else
        {
            pose = (bone.InitialTranslation, bone.InitialRotation);
        }

        cache[index] = pose;
        return pose;
    }

    private void UpdateBodyVelocity(int boneIndex, Vector3 worldPos, Quaternion worldRot, ref BodyReference body)
    {
        if (_prevBonePoses.TryGetValue(boneIndex, out var prev))
        {
            var linear = (worldPos - prev.Pos) / _lastDt;
            var delta = worldRot * Quaternion.Conjugate(prev.Rot);
            delta = Quaternion.Normalize(delta);
            if (delta.W < 0f)
                delta = new Quaternion(-delta.X, -delta.Y, -delta.Z, -delta.W);
            var axis = new Vector3(delta.X, delta.Y, delta.Z);
            var sinHalf = axis.Length();
            if (sinHalf > 1e-5f)
                axis /= sinHalf;
            else
                axis = Vector3.Zero;
            var angle = 2f * MathF.Atan2(sinHalf, delta.W);
            body.Velocity.Linear = linear;
            body.Velocity.Angular = axis * angle / _lastDt;
        }
        else
        {
            body.Velocity.Linear = Vector3.Zero;
            body.Velocity.Angular = Vector3.Zero;
        }
        _prevBonePoses[boneIndex] = (worldPos, worldRot);
    }

    public void SyncToBones(Scene scene)
    {
        if (_skipSimulation)
        {
            _cloth.SyncToBones(scene);
            return;
        }
        if (_simulation is not null)
        {
            _poseMap.Clear();
            _matrixCache.Clear();
            _ikLinkMap.Clear();
            var mapSnapshot = _bodyBoneMap.ToArray();
            foreach (var pair in mapSnapshot)
            {
                var body = _simulation.Bodies.GetBodyReference(pair.Key);
                _poseMap[pair.Value.Bone] = (body.Pose.Position, body.Pose.Orientation);
            }

            foreach (var b in scene.Bones)
            {
                var ik = b.Ik;
                if (ik == null)
                    continue;
                foreach (var link in ik.Links)
                    _ikLinkMap[link.BoneIndex] = link;
            }
            foreach (var pair in mapSnapshot)
            {
                var info = pair.Value;
                if (info.Bone < 0 || info.Bone >= scene.Bones.Count)
                    continue;
                if (info.Mode == RigidBodyMode.FollowBone)
                    continue;
                var bone = scene.Bones[info.Bone];

                if (_config.LockTranslation)
                {
                    bone.Rotation = bone.InitialRotation;
                    bone.Translation = bone.InitialTranslation;
                    scene.Bones[info.Bone] = bone;
                    continue;
                }

                var pose = _poseMap[info.Bone];
                Quaternion localRot;
                Matrix4x4 parentWorld = default;
                Matrix4x4 invParent = default;
                var hasParent = bone.Parent >= 0;
                if (hasParent)
                {
                    parentWorld = GetWorldMatrix(scene, bone.Parent, _poseMap, _matrixCache);
                    Matrix4x4.Invert(parentWorld, out invParent);
                    var world = Matrix4x4.CreateFromQuaternion(pose.Rot) * Matrix4x4.CreateTranslation(pose.Pos);
                    var local = invParent * world;
                    Matrix4x4.Decompose(local, out _, out localRot, out _);
                }
                else
                {
                    localRot = pose.Rot;
                }

                if (_ikLinkMap.TryGetValue(info.Bone, out var ikLink) && ikLink.HasLimit)
                {
                    var delta = localRot * Quaternion.Conjugate(bone.InitialRotation);
                    Quaternion basisRot = default;
                    if (bone.HasLocalAxis)
                    {
                        var x = Vector3.Normalize(bone.LocalAxisX);
                        var z = Vector3.Normalize(bone.LocalAxisZ);
                        var y = Vector3.Normalize(Vector3.Cross(z, x));
                        // PMX仕様ではローカル軸は右手系で定義されており、
                        // 指定されたX軸とZ軸からY軸を z × x で求める。
                        // Matrix4x4 は列ベクトルが基底となるため、
                        // 各軸ベクトルを列として配置する必要がある。
                        basisRot = Quaternion.CreateFromRotationMatrix(new Matrix4x4(
                            x.X, y.X, z.X, 0f,
                            x.Y, y.Y, z.Y, 0f,
                            x.Z, y.Z, z.Z, 0f,
                            0f, 0f, 0f, 1f));
                        delta = Quaternion.Normalize(Quaternion.Conjugate(basisRot) * delta * basisRot);
                    }

                    var euler = ToEulerZxy(delta);
                    euler = Vector3.Clamp(euler, ikLink.MinAngle, ikLink.MaxAngle);
                    delta = FromEulerZxy(euler);

                    if (bone.HasLocalAxis)
                        delta = Quaternion.Normalize(basisRot * delta * Quaternion.Conjugate(basisRot));

                    localRot = Quaternion.Normalize(delta * bone.InitialRotation);
                }

                if (info.Mode == RigidBodyMode.PhysicsWithBoneAlignment)
                {
                    bone.Rotation = Quaternion.Slerp(bone.Rotation, localRot, BoneBlendFactor);
                }
                else
                {
                    bone.Rotation = localRot;
                }

                Vector3 localTrans;
                if (hasParent)
                {
                    var initialWorld = Vector3.Transform(bone.InitialTranslation, parentWorld);
                    var deltaWorld = pose.Pos - initialWorld;
                    localTrans = bone.InitialTranslation + Vector3.TransformNormal(deltaWorld, invParent);
                }
                else
                {
                    var deltaWorld = pose.Pos - bone.InitialTranslation;
                    localTrans = bone.InitialTranslation + deltaWorld;
                }

                if (info.Mode == RigidBodyMode.PhysicsWithBoneAlignment)
                {
                    bone.Translation = Vector3.Lerp(bone.Translation, localTrans, BoneBlendFactor);
                }
                else
                {
                    bone.Translation = localTrans;
                }

                scene.Bones[info.Bone] = bone;
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
            mat = GetWorldMatrix(scene, boneData.Parent, poses, cache) * local;
        else
            mat = local;

        cache[index] = mat;
        return mat;
    }

    public void Dispose()
    {
        _skipSimulation = true;
        _simulation?.Dispose();
        _simulation = null;

        _bufferPool?.Clear();
        _bufferPool = null;
        _threadDispatcher?.Dispose();
        _threadDispatcher = null;
        _staticMaterialMap.Clear();
        _staticFilterMap.Clear();
        _originalInertiaMap.Clear();
    }

    public void LoadRigidBodies(ModelData model)
    {
        _skipSimulation = false;
        if (_simulation is null) return;
        foreach (var handle in _rigidBodyHandles)
        {
            _simulation.Bodies.Remove(handle);
        }
        foreach (var shape in _shapeIndices)
        {
            _simulation.Shapes.Remove(shape);
        }
        _rigidBodyHandles.Clear();
        _shapeIndices.Clear();
        _materialMap.Clear();
        _bodyFilterMap.Clear();
        _dampingMap.Clear();
        _prevBonePoses.Clear();
        _originalInertiaMap.Clear();

        var processed = 0;
        for (int i = 0; i < model.RigidBodies.Count; i++)
        {
            var rb = model.RigidBodies[i];
            if (!IsValidRigidBody(rb))
            {
                _logger.LogWarning("異常値の剛体をスキップ: {Index}:{Name}", i, rb.Name);
                AppendCrashLog($"Invalid rigid body skipped: {i}:{rb.Name}");
                continue;
            }

            var mass = rb.Mass;
            TypedIndex shapeIndex;
            BodyInertia inertia = default;
            switch (rb.Shape)
            {
                case RigidBodyShape.Sphere:
                    var sphere = new Sphere(rb.Size.X);
                    shapeIndex = _simulation.Shapes.Add(sphere);
                    if (mass > 0f)
                        inertia = sphere.ComputeInertia(mass);
                    break;
                case RigidBodyShape.Capsule:
                    var capsule = new Capsule(rb.Size.X, rb.Size.Y);
                    shapeIndex = _simulation.Shapes.Add(capsule);
                    if (mass > 0f)
                        inertia = capsule.ComputeInertia(mass);
                    break;
                case RigidBodyShape.Box:
                    var box = new Box(rb.Size.X, rb.Size.Y, rb.Size.Z);
                    shapeIndex = _simulation.Shapes.Add(box);
                    if (mass > 0f)
                        inertia = box.ComputeInertia(mass);
                    break;
                default:
                    _logger.LogWarning("未知の剛体形状をスキップ: {Index}:{Name}", i, rb.Name);
                    continue;
            }
            _shapeIndices.Add(shapeIndex);

            var pose = new RigidPose(rb.Position,
                FromEulerZxy(rb.Rotation));
            // Group はビット位置なので 1u << rb.Group に変換
            // Mask は「衝突させたくないグループ」なのでビット反転 (~rb.Mask & 0xFFFF)
            var filter = new SubgroupCollisionFilter(1u << rb.Group, (uint)(~rb.Mask & 0xFFFF));
            var collidable = new CollidableDescription(shapeIndex, 0.1f);

            BodyDescription bodyDesc;
            if (mass == 0f)
            {
                bodyDesc = BodyDescription.CreateKinematic(pose, collidable, new BodyActivityDescription());
                _logger.LogWarning("質量0の剛体を静的剛体として処理しました: {Index}:{Name}", i, rb.Name);
            }
            else if (rb.Mode == RigidBodyMode.FollowBone)
            {
                bodyDesc = BodyDescription.CreateKinematic(pose, collidable, new BodyActivityDescription());
            }
            else
            {
                bodyDesc = BodyDescription.CreateDynamic(pose, inertia, collidable, new BodyActivityDescription());
            }

            var handle = _simulation.Bodies.Add(bodyDesc);
            _rigidBodyHandles.Add(handle);
            _bodyBoneMap[handle] = (rb.BoneIndex, mass == 0f ? RigidBodyMode.FollowBone : rb.Mode);
            var maxRecovery = ComputeMaxRecoveryVelocity(rb.Restitution, _config.RestitutionRecoveryScale);
            _materialMap[handle] = new Material(rb.Friction, maxRecovery);
            _bodyFilterMap[handle] = filter;
            if (rb.LinearDamping != _config.Damping || rb.AngularDamping != _config.Damping)
                _dampingMap[handle] = (rb.LinearDamping, rb.AngularDamping);

            processed++;
        }

        if (_bufferPool is not null && _threadDispatcher is not null)
        {
            var context = new BepuPhysics.Trees.Tree.RefitAndRefineMultithreadedContext();
            context.RefitAndRefine(ref _simulation.BroadPhase.ActiveTree, _bufferPool, _threadDispatcher, _frameIndex++);
            context.CleanUpForRefitAndRefine(_bufferPool);
        }

        _logger.LogInformation("剛体ロード完了: 合計 {Count} 個, BroadPhase葉数={LeafCount}", processed, _simulation.BroadPhase.ActiveTree.LeafCount);

        static bool IsValidRigidBody(RigidBodyData rb)
        {
            if (rb.Mass < 0 || float.IsNaN(rb.Mass) || float.IsInfinity(rb.Mass))
            {
                return false;
            }

            static bool Invalid(float v) => v <= 0 || float.IsNaN(v) || float.IsInfinity(v);
            if (!(float.IsFinite(rb.Position.X) && float.IsFinite(rb.Position.Y) && float.IsFinite(rb.Position.Z) &&
                  float.IsFinite(rb.Rotation.X) && float.IsFinite(rb.Rotation.Y) && float.IsFinite(rb.Rotation.Z)))
            {
                return false;
            }
            return rb.Shape switch
            {
                RigidBodyShape.Sphere => !Invalid(rb.Size.X),
                RigidBodyShape.Capsule => !Invalid(rb.Size.X) && !Invalid(rb.Size.Y),
                RigidBodyShape.Box => !Invalid(rb.Size.X) && !Invalid(rb.Size.Y) && !Invalid(rb.Size.Z),
                _ => false,
            };
        }
    }

    public void LoadSoftBodies(ModelData model)
    {
        _cloth.Nodes.Clear();
        _cloth.Springs.Clear();
        _cloth.BoneMap.Clear();
        _cloth.ClearColliders();

        foreach (var rb in model.RigidBodies)
        {
            switch (rb.Shape)
            {
                case RigidBodyShape.Sphere:
                    _cloth.AddSphereCollider(rb.Position, rb.Size.X);
                    break;
                case RigidBodyShape.Capsule:
                    var rot = FromEulerZxy(rb.Rotation);
                    var dir = Vector3.Transform(Vector3.UnitY, rot);
                    var half = rb.Size.Y * 0.5f;
                    var a = rb.Position + dir * half;
                    var b = rb.Position - dir * half;
                    _cloth.AddCapsuleCollider(a, b, rb.Size.X);
                    break;
            }
        }

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

        var nodeCount = CountSoftBodyNodes(model, rootIndex);
        var nodeMass = sb.NodeMassIsTotal ? sb.NodeMass / Math.Max(nodeCount, 1) : sb.NodeMass;
        if (nodeMass <= 0f)
            nodeMass = 1f;
        var mass = nodeMass;

        var rootBone = model.Bones[rootIndex];
        var rootPos = rootBone.Translation;
        var rootRot = rootBone.Rotation;

        var queue = new Queue<(int Bone, int ParentNode, Vector3 WorldPos, Quaternion WorldRot)>();
        queue.Enqueue((rootIndex, -1, rootPos, rootRot));

        while (queue.Count > 0)
        {
            var (boneIndex, parentNode, worldPos, worldRot) = queue.Dequeue();
            var nodeIndex = _cloth.Nodes.Count;
            var invMass = parentNode < 0 ? 0f : 1f / mass;
            _cloth.Nodes.Add(new ClothNode { Position = worldPos, PrevPosition = worldPos, Velocity = Vector3.Zero, InverseMass = invMass });
            _cloth.BoneMap.Add(boneIndex);

            if (parentNode >= 0)
            {
                var prevPos = _cloth.Nodes[parentNode].Position;
                var rest = Vector3.Distance(prevPos, worldPos);
                if (rest > 0f)
                {
                    _cloth.Springs.Add(new Spring
                    {
                        NodeA = parentNode,
                        NodeB = nodeIndex,
                        RestLength = rest,
                        Stiffness = sb.SpringStiffness,
                        Damping = sb.SpringDamping
                    });
                }
            }

            if (model.BoneChildren.TryGetValue(boneIndex, out var childIndices))
            {
                foreach (var childIndex in childIndices)
                {
                    var child = model.Bones[childIndex];
                    var childWorldPos = Vector3.Transform(child.Translation, worldRot) + worldPos;
                    var childWorldRot = child.Rotation * worldRot;
                    queue.Enqueue((childIndex, nodeIndex, childWorldPos, childWorldRot));
                }
            }
        }
    }

    private int CountSoftBodyNodes(ModelData model, int rootIndex)
    {
        var count = 0;
        var queue = new Queue<int>();
        queue.Enqueue(rootIndex);
        while (queue.Count > 0)
        {
            var boneIndex = queue.Dequeue();
            count++;
            if (model.BoneChildren.TryGetValue(boneIndex, out var childIndices))
            {
                foreach (var childIndex in childIndices)
                    queue.Enqueue(childIndex);
            }
        }
        return count;
    }

    public void LoadJoints(ModelData model)
    {
        if (model.Joints.Count == 0)
        {
            _logger.LogWarning(
                "モデル {ModelName} はジョイントが定義されていません（剛体数: {RigidBodyCount}）。ジョイントが欠落しているためシミュレーションをスキップします。",
                model.ModelName,
                model.RigidBodies.Count);
            _skipSimulation = true;
            return;
        }
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
        // PMX仕様では min > max の場合は「制限なし」と解釈する
        if (min > max) return;
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
        // PMX仕様では min > max の場合は「制限なし」と解釈する
        if (min > max) return;
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

    private static Vector3 ToEulerZxy(Quaternion q)
    {
        var m = Matrix4x4.CreateFromQuaternion(q);
        var x = MathF.Asin(Math.Clamp(m.M32, -1f, 1f));
        float y, z;
        if (MathF.Abs(m.M32) < 0.999999f)
        {
            z = MathF.Atan2(-m.M12, m.M22);
            y = MathF.Atan2(-m.M31, m.M33);
        }
        else
        {
            z = MathF.Atan2(m.M21, m.M11);
            y = 0f;
        }
        return new Vector3(x, y, z);
    }

    private static Quaternion FromEulerZxy(Vector3 rot)
    {
        var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rot.Z);
        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rot.X);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rot.Y);
        return Quaternion.Normalize(qy * qx * qz);
    }

    private static void ComputeJointLocalPoses(JointData joint, RigidBodyData a, RigidBodyData b,
        out RigidPose localA, out RigidPose localB)
    {
        var jointOrientation = FromEulerZxy(joint.Rotation);
        localA = ToLocalPose(joint.Position, jointOrientation, a);
        localB = ToLocalPose(joint.Position, jointOrientation, b);
    }

    private static RigidPose ToLocalPose(Vector3 jointPos, Quaternion jointOrientation, RigidBodyData rb)
    {
        var bodyOrientation = FromEulerZxy(rb.Rotation);
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

    private void RefitAndRefineBroadPhase()
    {
        if (_simulation is null || _bufferPool is null || _threadDispatcher is null)
            return;
        try
        {
            var context = new BepuPhysics.Trees.Tree.RefitAndRefineMultithreadedContext();
            context.RefitAndRefine(ref _simulation.BroadPhase.ActiveTree, _bufferPool, _threadDispatcher, _frameIndex++);
            context.CleanUpForRefitAndRefine(_bufferPool);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ActiveTree.RefitAndRefine に失敗しました。");
            _skipSimulation = true;
        }
    }

    private void LogBodyAabbs(LogLevel level)
    {
        if (_simulation is null)
            return;

        foreach (var handle in _rigidBodyHandles)
        {
            if (!_simulation.Bodies.BodyExists(handle))
                continue;

            var bounds = _simulation.Bodies.GetBodyReference(handle).BoundingBox;
            _logger.Log(level, "剛体状態: {Index}, Min={Min}, Max={Max}", handle.Value, bounds.Min, bounds.Max);
        }
    }

    private void LogCollidablePairs(LogLevel level)
    {
        if (_simulation is null)
            return;

        foreach (var entry in _simulation.NarrowPhase.PairCache.Mapping)
        {
            var pair = entry.Key;
            _logger.Log(level, "ペア状態: A={A}, B={B}", pair.A, pair.B);
        }
    }

    private static bool NeedsBroadPhaseRebuild(int bodyCount, int leafCount)
    {
        var diff = Math.Abs(bodyCount - leafCount);
        return diff > 32 && diff > bodyCount / 2;
    }

    /// <summary>
    /// Restitution (0～1) から最大反発速度を算出する。
    /// </summary>
    private static float ComputeMaxRecoveryVelocity(float restitution, float scale)
    {
        return Math.Clamp(restitution, 0f, 1f) * scale;
    }

    private static void AppendCrashLog(string message, Exception? ex = null)
    {
        try
        {
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(directory)) directory = AppContext.BaseDirectory;
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "crash_log.txt");

            const long maxSize = 1 * 1024 * 1024;
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                if (info.Length > maxSize)
                {
                    var archive = Path.Combine(directory, $"crash_log_{DateTime.Now:yyyyMMddHHmmss}.txt");
                    File.Move(path, archive);
                    var archives = Directory.GetFiles(directory, "crash_log_*.txt")
                        .OrderByDescending(f => f)
                        .Skip(5);
                    foreach (var old in archives) File.Delete(old);
                }
            }

            var text = $"[{DateTime.Now:O}] {message}";
            if (ex != null) text += $" {ex}";
            File.AppendAllText(path, text + Environment.NewLine);
        }
        catch
        {
        }
    }

    private struct SubgroupFilteredCallbacks : INarrowPhaseCallbacks
    {
        private readonly Dictionary<BodyHandle, Material> _materials;
        private readonly Dictionary<BodyHandle, SubgroupCollisionFilter> _filters;
        private readonly Dictionary<StaticHandle, Material> _staticMaterials;
        private readonly Dictionary<StaticHandle, SubgroupCollisionFilter> _staticFilters;

        public SubgroupFilteredCallbacks(Dictionary<BodyHandle, Material> materials,
            Dictionary<BodyHandle, SubgroupCollisionFilter> filters,
            Dictionary<StaticHandle, Material> staticMaterials,
            Dictionary<StaticHandle, SubgroupCollisionFilter> staticFilters)
        {
            _materials = materials;
            _filters = filters;
            _staticMaterials = staticMaterials;
            _staticFilters = staticFilters;
        }

        public void Initialize(Simulation simulation) { }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            var filterA = GetFilter(a);
            var filterB = GetFilter(b);
            return SubgroupCollisionFilter.AllowCollision(filterA, filterB);
        }

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
            out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            var matA = GetMaterial(pair.A);
            var matB = GetMaterial(pair.B);
            var friction = (matA.Friction + matB.Friction) * 0.5f;
            var maxRecovery = (matA.MaximumRecoveryVelocity + matB.MaximumRecoveryVelocity) * 0.5f;
            pairMaterial = new PairMaterialProperties(friction, maxRecovery, new SpringSettings(30f, 1f));
            return true;
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
            ref ConvexContactManifold manifold) => true;

        private SubgroupCollisionFilter GetFilter(CollidableReference collidable)
        {
            if (collidable.Mobility == CollidableMobility.Static)
            {
                _staticFilters.TryGetValue(collidable.StaticHandle, out var filter);
                return filter;
            }

            _filters.TryGetValue(collidable.BodyHandle, out var dynamicFilter);
            return dynamicFilter;
        }

        private Material GetMaterial(CollidableReference collidable)
        {
            if (collidable.Mobility == CollidableMobility.Static)
            {
                _staticMaterials.TryGetValue(collidable.StaticHandle, out var mat);
                return mat;
            }

            _materials.TryGetValue(collidable.BodyHandle, out var dynamicMat);
            return dynamicMat;
        }

        public void Dispose() { }
    }

    private struct Material
    {
        public float Friction;
        public float MaximumRecoveryVelocity;

        public Material(float friction, float maximumRecoveryVelocity)
        {
            Friction = friction;
            MaximumRecoveryVelocity = maximumRecoveryVelocity;
        }
    }

    private struct SimplePoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        public Vector3 Gravity;
        private readonly Dictionary<BodyHandle, (float Linear, float Angular)> _damping;
        private readonly float _defaultLinear;
        private readonly float _defaultAngular;
        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
        public bool AllowSubstepsForUnconstrainedBodies => false;
        public bool IntegrateVelocityForKinematics => false;
        public SimplePoseIntegratorCallbacks(Vector3 gravity, float linearDamping, float angularDamping,
            Dictionary<BodyHandle, (float Linear, float Angular)> damping)
        {
            Gravity = gravity;
            _defaultLinear = linearDamping;
            _defaultAngular = angularDamping;
            _damping = damping;
        }

        public void Initialize(Simulation simulation) { }

        public void PrepareForIntegration(float dt) { }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
            BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            Vector3Wide.Broadcast(Gravity, out var g);
            velocity.Linear += g * dt;

            var linearX = velocity.Linear.X;
            var linearY = velocity.Linear.Y;
            var linearZ = velocity.Linear.Z;
            var angularX = velocity.Angular.X;
            var angularY = velocity.Angular.Y;
            var angularZ = velocity.Angular.Z;

            for (int i = 0; i < Vector<float>.Count; i++)
            {
                var handle = new BodyHandle(bodyIndices[i]);
                (float Linear, float Angular) d;
                if (!_damping.TryGetValue(handle, out d))
                    d = (_defaultLinear, _defaultAngular);
                var ldt = MathF.Pow(d.Linear, dt[i]);
                var adt = MathF.Pow(d.Angular, dt[i]);
                linearX = linearX.WithElement(i, linearX[i] * ldt);
                linearY = linearY.WithElement(i, linearY[i] * ldt);
                linearZ = linearZ.WithElement(i, linearZ[i] * ldt);
                angularX = angularX.WithElement(i, angularX[i] * adt);
                angularY = angularY.WithElement(i, angularY[i] * adt);
                angularZ = angularZ.WithElement(i, angularZ[i] * adt);
            }

            velocity.Linear.X = linearX;
            velocity.Linear.Y = linearY;
            velocity.Linear.Z = linearZ;
            velocity.Angular.X = angularX;
            velocity.Angular.Y = angularY;
            velocity.Angular.Z = angularZ;
        }
    }
}
