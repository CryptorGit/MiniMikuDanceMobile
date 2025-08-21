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

    private Vector3 _gravity = new(0, -9.81f, 0);
    private float _damping = 0.98f;

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

    public void Step(float dt)
    {
        if (Nodes.Count == 0)
            return;

        var forces = new Vector3[Nodes.Count];

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

        for (int i = 0; i < Nodes.Count; i++)
        {
            var node = Nodes[i];
            if (node.InverseMass <= 0f)
                continue;
            var accel = _gravity + forces[i] * node.InverseMass;
            node.Velocity += accel * dt;
            node.Velocity *= _damping;
            node.Position += node.Velocity * dt;
            Nodes[i] = node;
        }
    }

    public void SyncToBones(Scene scene)
    {
        var count = Math.Min(Nodes.Count, BoneMap.Count);
        var worldCache = new Dictionary<int, (Vector3 Pos, Quaternion Rot)>();

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

            if (bone.Parent >= 0)
            {
                var parentPose = GetWorldPose(bone.Parent);
                parentRot = parentPose.Rot;
            }

            if (i + 1 < count)
            {
                var nextPos = Nodes[i + 1].Position;
                var dir = nextPos - nodePos;
                if (dir.LengthSquared() > 1e-8f && bone.BaseForward.LengthSquared() > 1e-8f)
                {
                    var baseDir = Vector3.Normalize(bone.BaseForward);
                    dir = Vector3.Normalize(dir);
                    var dot = Math.Clamp(Vector3.Dot(baseDir, dir), -1f, 1f);
                    var axis = Vector3.Cross(dir, baseDir);
                    Quaternion delta;
                    if (axis.LengthSquared() > 1e-8f)
                    {
                        axis = Vector3.Normalize(axis);
                        var angle = MathF.Acos(dot);
                        delta = Quaternion.CreateFromAxisAngle(axis, angle);
                    }
                    else
                    {
                        delta = dot > 0f ? Quaternion.Identity : Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
                    }
                    bone.Rotation = delta * bone.InitialRotation;
                }
            }

            scene.Bones[boneIndex] = bone;
            worldCache[boneIndex] = (nodePos, bone.Rotation * parentRot);
        }
    }
}

