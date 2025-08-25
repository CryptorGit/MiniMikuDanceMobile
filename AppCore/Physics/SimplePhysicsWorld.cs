using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.App;
using MiniMikuDance.Import;

namespace MiniMikuDance.Physics;

public sealed class SimplePhysicsWorld : IPhysicsWorld
{
    public List<RigidBody> RigidBodies { get; } = new();
    public List<Joint> Joints { get; } = new();
    private Vector3 _gravity = new(0f, -9.81f, 0f);

    public void Load(ModelData model)
    {
        RigidBodies.Clear();
        foreach (var rb in model.RigidBodies)
        {
            RigidBodies.Add(new RigidBody
            {
                Name = rb.Name,
                NameEnglish = rb.NameEnglish,
                BoneIndex = rb.BoneIndex,
                Mass = rb.Mass,
                Shape = rb.Shape,
                TranslationAttenuation = rb.TranslationAttenuation,
                RotationAttenuation = rb.RotationAttenuation,
                Recoil = rb.Recoil,
                Friction = rb.Friction,
                Position = rb.Position,
                Rotation = rb.Rotation,
                Size = rb.Size,
                Group = rb.Group,
                GroupTarget = rb.GroupTarget,
                PhysicsType = rb.PhysicsType
            });
        }
        Joints.Clear();
        foreach (var j in model.Joints)
        {
            Joints.Add(new Joint
            {
                Name = j.Name,
                RigidBodyA = j.RigidBodyA,
                RigidBodyB = j.RigidBodyB,
                Position = j.Position,
                Rotation = j.Rotation,
                PositionMin = j.PositionMin,
                PositionMax = j.PositionMax,
                RotationMin = j.RotationMin,
                RotationMax = j.RotationMax,
                SpringPosition = j.SpringPosition,
                SpringRotation = j.SpringRotation
            });
        }
    }

    public void Initialize(float modelScale) { }

    public void Step(float dt) { }

    public void SyncFromBones(Scene scene) { }

    public void SyncToBones(Scene scene) { }

    public Vector3 GetGravity() => _gravity;

    public void Dispose() { }
}
