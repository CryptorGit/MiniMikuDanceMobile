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
        var count = System.Math.Min(Nodes.Count, BoneMap.Count);
        for (int i = 0; i < count; i++)
        {
            var boneIndex = BoneMap[i];
            if (boneIndex < 0 || boneIndex >= scene.Bones.Count)
                continue;
            var bone = scene.Bones[boneIndex];
            bone.Translation = Nodes[i].Position;
        }
    }
}

