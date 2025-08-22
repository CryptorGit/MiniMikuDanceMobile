using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.App;

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

    private Vector3 _gravity = new(0, -9.81f, 0);
    // 1秒あたりの速度減衰率 (0～1)
    private float _damping = 0.98f;
    private float _groundHeight = 0f;
    private float _restitution = 0.2f;
    private float _friction = 0.5f;
    private Vector3[] _forceBuffer = Array.Empty<Vector3>();

    public int Substeps { get; set; } = 1;

    public Vector3 Gravity
    {
        get => _gravity;
        set => _gravity = value;
    }

    public float Damping
    {
        get => _damping;
        set => _damping = value;
    }

    public float GroundHeight
    {
        get => _groundHeight;
        set => _groundHeight = value;
    }

    public float Restitution
    {
        get => _restitution;
        set => _restitution = value;
    }

    public float Friction
    {
        get => _friction;
        set => _friction = value;
    }

    public void Step(float dt)
    {
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
        var worldCache = new Dictionary<int, (Vector3 Pos, Quaternion Rot)>();
        var boneToNode = new Dictionary<int, int>();
        for (int i = 0; i < count; i++)
            boneToNode[BoneMap[i]] = i;

        (Vector3 Pos, Quaternion Rot) GetWorldPose(int idx)
        {
            if (worldCache.TryGetValue(idx, out var pose))
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
            worldCache[idx] = pose;
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
                parentNodePos = boneToNode.TryGetValue(bone.Parent, out var pn)
                    ? Nodes[pn].Position : parentPose.Pos;
            }

            var dirWorld = nodePos - parentNodePos;
            var len = bone.InitialTranslation.Length();
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
            worldCache[boneIndex] = (worldPos, bone.Rotation * parentRot);
        }
    }
}

