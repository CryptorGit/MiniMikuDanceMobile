using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.App;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiniMikuDance.Physics.Cloth;

public class ClothSimulator
{
    public List<Node> Nodes { get; } = new();
    public List<Spring> Springs { get; } = new();
    public List<int> BoneMap { get; } = new();

    public struct SphereCollider
    {
        public Vector3 Center;
        public float Radius;
    }

    public struct CapsuleCollider
    {
        public Vector3 PointA;
        public Vector3 PointB;
        public float Radius;
    }

    public List<SphereCollider> SphereColliders { get; } = new();
    public List<CapsuleCollider> CapsuleColliders { get; } = new();

    private readonly ILogger _logger;

    public ClothSimulator(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public void ClearColliders()
    {
        SphereColliders.Clear();
        CapsuleColliders.Clear();
    }

    public void AddSphereCollider(Vector3 center, float radius)
    {
        if (radius <= 0f)
        {
            _logger.LogWarning("SphereCollider の radius は 0 より大きい必要があります。入力値 {Radius} を無視します。", radius);
            return;
        }

        SphereColliders.Add(new SphereCollider { Center = center, Radius = radius });
    }

    public void AddCapsuleCollider(Vector3 pointA, Vector3 pointB, float radius)
    {
        if (radius <= 0f)
        {
            _logger.LogWarning("CapsuleCollider の radius は 0 より大きい必要があります。入力値 {Radius} を無視します。", radius);
            return;
        }

        if (pointA == pointB)
        {
            _logger.LogWarning("CapsuleCollider の pointA と pointB が同一点です。入力を無視します。");
            return;
        }

        CapsuleColliders.Add(new CapsuleCollider { PointA = pointA, PointB = pointB, Radius = radius });
    }

    private Vector3 _gravity = new(0, -9.81f, 0);
    // 1秒あたりの速度減衰率 (0～1)
    private float _damping = 0.98f;
    private float _groundHeight = 0f;
    private float _restitution = 0.2f;
    private float _friction = 0.5f;
    private Vector3[] _forceBuffer = Array.Empty<Vector3>();
    private readonly Dictionary<int, (Vector3 Pos, Quaternion Rot)> _worldCache = new();
    private readonly Dictionary<int, int> _boneToNode = new();

    public int Substeps { get; set; } = 1;

    public bool LockTranslation { get; set; } = false;

    public Vector3 Gravity
    {
        get => _gravity;
        set => _gravity = value;
    }

    public float Damping
    {
        get => _damping;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (clamped != value)
                _logger.LogWarning("Damping は 0～1 の範囲に収めてください。入力値 {Value} を {Clamped} に補正します。", value, clamped);
            _damping = clamped;
        }
    }

    public float GroundHeight
    {
        get => _groundHeight;
        set => _groundHeight = value;
    }

    public float Restitution
    {
        get => _restitution;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (clamped != value)
                _logger.LogWarning("Restitution は 0～1 の範囲に収めてください。入力値 {Value} を {Clamped} に補正します。", value, clamped);
            _restitution = clamped;
        }
    }

    public float Friction
    {
        get => _friction;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (clamped != value)
                _logger.LogWarning("Friction は 0～1 の範囲に収めてください。入力値 {Value} を {Clamped} に補正します。", value, clamped);
            _friction = clamped;
        }
    }

    public void Step(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0f)
        {
            _logger.LogWarning("dt が不正な値のため Step をスキップします: {Dt}", dt);
            return;
        }
        if (Nodes.Count == 0)
            return;

        if (_forceBuffer.Length < Nodes.Count)
            _forceBuffer = new Vector3[Nodes.Count];

        var steps = Math.Max(1, Substeps);
        var subDt = dt / steps;

        for (int step = 0; step < steps; step++)
        {
            Array.Clear(_forceBuffer, 0, Nodes.Count);
            var forces = _forceBuffer;

            foreach (var spring in Springs)
            {
                var aIndex = spring.NodeA;
                var bIndex = spring.NodeB;
                if (aIndex < 0 || aIndex >= Nodes.Count || bIndex < 0 || bIndex >= Nodes.Count)
                    continue;

                var nodeA = Nodes[aIndex];
                var nodeB = Nodes[bIndex];
                var delta = nodeB.Position - nodeA.Position;
                var length = delta.Length();
                if (length <= 1e-6f)
                    continue;

                var dir = delta / length;
                var relativeVel = Vector3.Dot(nodeB.Velocity - nodeA.Velocity, dir);
                var forceMag = (length - spring.RestLength) * spring.Stiffness + relativeVel * spring.Damping;
                var force = dir * forceMag;
                forces[aIndex] += force;
                forces[bIndex] -= force;
            }

            var damping = MathF.Pow(_damping, subDt);
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if (node.InverseMass <= 0f)
                {
                    node.PrevPosition = node.Position;
                    node.Velocity = Vector3.Zero;
                    Nodes[i] = node;
                    continue;
                }

                var accel = _gravity + forces[i] * node.InverseMass;
                var current = node.Position;
                var nextPos = current + (current - node.PrevPosition) * damping + accel * subDt * subDt;
                var newVelocity = (nextPos - current) / subDt;
                var collided = false;

                if (nextPos.Y < _groundHeight)
                {
                    collided = true;
                    nextPos.Y = _groundHeight;
                    if (newVelocity.Y < 0f)
                        newVelocity.Y = -newVelocity.Y * _restitution;
                    newVelocity.X *= _friction;
                    newVelocity.Z *= _friction;
                }

                foreach (var sphere in SphereColliders)
                {
                    var toNode = nextPos - sphere.Center;
                    var dist = toNode.Length();
                    if (dist < sphere.Radius)
                    {
                        collided = true;
                        var normal = dist > 1e-6f ? toNode / dist : Vector3.UnitY;
                        nextPos = sphere.Center + normal * sphere.Radius;
                        var vn = Vector3.Dot(newVelocity, normal);
                        if (vn < 0f)
                        {
                            var vt = newVelocity - vn * normal;
                            newVelocity = vt * _friction - vn * _restitution * normal;
                        }
                    }
                }

                foreach (var capsule in CapsuleColliders)
                {
                    var ab = capsule.PointB - capsule.PointA;
                    var abLenSq = ab.LengthSquared();
                    if (abLenSq < 1e-8f)
                        continue;
                    var t = Math.Clamp(Vector3.Dot(nextPos - capsule.PointA, ab) / abLenSq, 0f, 1f);
                    var closest = capsule.PointA + ab * t;
                    var toNode = nextPos - closest;
                    var dist = toNode.Length();
                    if (dist < capsule.Radius)
                    {
                        collided = true;
                        var normal = dist > 1e-6f ? toNode / dist : Vector3.UnitY;
                        nextPos = closest + normal * capsule.Radius;
                        var vn = Vector3.Dot(newVelocity, normal);
                        if (vn < 0f)
                        {
                            var vt = newVelocity - vn * normal;
                            newVelocity = vt * _friction - vn * _restitution * normal;
                        }
                    }
                }

                if (collided)
                    node.PrevPosition = nextPos - newVelocity * subDt;
                else
                    node.PrevPosition = current;

                node.Position = nextPos;
                node.Velocity = newVelocity;
                Nodes[i] = node;
            }
        }
    }

    public void SyncToBones(Scene scene)
    {
        var count = Math.Min(Nodes.Count, BoneMap.Count);
        _worldCache.Clear();
        _boneToNode.Clear();
        for (int i = 0; i < count; i++)
            _boneToNode[BoneMap[i]] = i;

        (Vector3 Pos, Quaternion Rot) GetWorldPose(int idx)
        {
            if (_worldCache.TryGetValue(idx, out var pose))
                return pose;
            var b = scene.Bones[idx];
            var pos = b.Translation;
            var rot = b.Rotation;
            if (b.Parent >= 0)
            {
                var parent = GetWorldPose(b.Parent);
                pos = Vector3.Transform(pos, parent.Rot) + parent.Pos;
                rot = rot * parent.Rot;
            }
            pose = (pos, rot);
            _worldCache[idx] = pose;
            return pose;
        }

        for (int i = 0; i < count; i++)
        {
            var boneIndex = BoneMap[i];
            if (boneIndex < 0 || boneIndex >= scene.Bones.Count)
                continue;

            var bone = scene.Bones[boneIndex];
            var nodePos = Nodes[i].Position;
            Quaternion parentRot = Quaternion.Identity;
            Vector3 parentBonePos = Vector3.Zero;
            Vector3 parentNodePos = Vector3.Zero;

            if (bone.Parent >= 0)
            {
                var parentPose = GetWorldPose(bone.Parent);
                parentRot = parentPose.Rot;
                parentBonePos = parentPose.Pos;
                parentNodePos = _boneToNode.TryGetValue(bone.Parent, out var pn)
                    ? Nodes[pn].Position : parentPose.Pos;
            }

            var dirWorld = nodePos - parentNodePos;
            var len = bone.InitialTranslation.Length();
            if (!LockTranslation)
            {
                if (dirWorld.LengthSquared() > 1e-8f)
                {
                    var invParentRot = Quaternion.Inverse(parentRot);
                    var dirLocal = Vector3.Normalize(Vector3.Transform(dirWorld, invParentRot));
                    bone.Translation = dirLocal * len;
                }
                else
                {
                    bone.Translation = bone.InitialTranslation;
                }
            }
            else
            {
                bone.Translation = bone.InitialTranslation;
            }

            int childIndex = -1;
            foreach (var spring in Springs)
            {
                var other = -1;
                if (spring.NodeA == i)
                    other = spring.NodeB;
                else if (spring.NodeB == i)
                    other = spring.NodeA;

                if (other < 0 || other >= Nodes.Count)
                    continue;

                if (other < BoneMap.Count)
                {
                    var childBoneIndex = BoneMap[other];
                    if (childBoneIndex >= 0 && childBoneIndex < scene.Bones.Count && scene.Bones[childBoneIndex].Parent == boneIndex)
                    {
                        childIndex = other;
                        break;
                    }
                }
            }

            if (childIndex >= 0)
            {
                var nextPos = Nodes[childIndex].Position;
                var dir = nextPos - nodePos;
                if (dir.LengthSquared() > 1e-8f && bone.BaseForward.LengthSquared() > 1e-8f)
                {
                    var baseDir = Vector3.Normalize(bone.BaseForward);
                    dir = Vector3.Normalize(dir);
                    var dot = Math.Clamp(Vector3.Dot(baseDir, dir), -1f, 1f);
                    var axis = Vector3.Cross(baseDir, dir);
                    Quaternion delta;
                    if (axis.LengthSquared() > 1e-8f)
                    {
                        axis = Vector3.Normalize(axis);
                        var angle = MathF.Acos(dot);
                        delta = Quaternion.CreateFromAxisAngle(axis, angle);
                    }
                    else
                    {
                        var upAxis = bone.BaseUp.LengthSquared() > 1e-8f ? Vector3.Normalize(bone.BaseUp) : Vector3.UnitY;
                        var angle = dot > 0f ? 0f : MathF.PI;
                        delta = Quaternion.CreateFromAxisAngle(upAxis, angle);
                    }
                    bone.Rotation = delta * bone.InitialRotation;
                }
            }

            scene.Bones[boneIndex] = bone;
            var worldPos = parentBonePos + Vector3.Transform(bone.Translation, parentRot);
            _worldCache[boneIndex] = (worldPos, bone.Rotation * parentRot);
        }
    }
}

