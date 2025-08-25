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

    public void Step(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0f)
        {
            return;
        }

        foreach (var rb in RigidBodies)
        {
            if (rb.PhysicsType == RigidBodyPhysicsType.FollowBone)
            {
                rb.Velocity = Vector3.Zero;
                rb.AngularVelocity = Vector3.Zero;
                continue;
            }

            rb.Velocity += _gravity * dt;
            rb.Velocity *= 1f - rb.TranslationAttenuation;
            rb.Position += rb.Velocity * dt;

            rb.AngularVelocity *= 1f - rb.RotationAttenuation;
            rb.Rotation += rb.AngularVelocity * dt;

            ResolveFloorCollision(rb);
        }

        for (var i = 0; i < RigidBodies.Count; i++)
        {
            for (var j = i + 1; j < RigidBodies.Count; j++)
            {
                ResolveBodyCollision(RigidBodies[i], RigidBodies[j]);
            }
        }
    }

    public void SyncFromBones(Scene scene)
    {
        foreach (var rb in RigidBodies)
        {
            if (rb.PhysicsType != RigidBodyPhysicsType.FollowBone)
            {
                continue;
            }

            if (rb.BoneIndex < 0 || rb.BoneIndex >= scene.Bones.Count)
            {
                continue;
            }

            var bone = scene.Bones[rb.BoneIndex];
            rb.Position = bone.Translation;
            rb.Rotation = ToEuler(bone.Rotation);
        }
    }

    public void SyncToBones(Scene scene)
    {
        foreach (var rb in RigidBodies)
        {
            if (rb.PhysicsType != RigidBodyPhysicsType.PhysicsWithBoneAlignment)
            {
                continue;
            }

            if (rb.BoneIndex < 0 || rb.BoneIndex >= scene.Bones.Count)
            {
                continue;
            }

            var bone = scene.Bones[rb.BoneIndex];
            bone.Translation = rb.Position;
            bone.Rotation = Quaternion.CreateFromYawPitchRoll(rb.Rotation.Y, rb.Rotation.X, rb.Rotation.Z);
        }
    }

    public Vector3 GetGravity() => _gravity;

    public void Dispose() { }

    private static Vector3 ToEuler(Quaternion q)
    {
        var sinRCosP = 2f * (q.W * q.X + q.Y * q.Z);
        var cosRCosP = 1f - 2f * (q.X * q.X + q.Y * q.Y);
        var roll = MathF.Atan2(sinRCosP, cosRCosP);

        var sinP = 2f * (q.W * q.Y - q.Z * q.X);
        sinP = Math.Clamp(sinP, -1f, 1f);
        var pitch = MathF.Asin(sinP);

        var sinYCosP = 2f * (q.W * q.Z + q.X * q.Y);
        var cosYCosP = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
        var yaw = MathF.Atan2(sinYCosP, cosYCosP);

        return new Vector3(pitch, yaw, roll);
    }

    private static float GetRadius(RigidBody rb)
    {
        return MathF.Max(rb.Size.X, MathF.Max(rb.Size.Y, rb.Size.Z)) * 0.5f;
    }

    private void ResolveFloorCollision(RigidBody rb)
    {
        if (rb.PhysicsType == RigidBodyPhysicsType.FollowBone)
        {
            return;
        }

        var radius = GetRadius(rb);
        var bottom = rb.Position.Y - radius;
        if (bottom < 0f)
        {
            rb.Position.Y -= bottom;
            var vn = rb.Velocity.Y;
            if (vn < 0f)
            {
                rb.Velocity.Y = -vn * rb.Recoil;
                var tangent = new Vector3(rb.Velocity.X, 0f, rb.Velocity.Z);
                rb.Velocity -= tangent * rb.Friction;
            }
        }
    }

    private void ResolveBodyCollision(RigidBody a, RigidBody b)
    {
        if (!ShouldCollide(a, b))
        {
            return;
        }

        var ra = GetRadius(a);
        var rbRadius = GetRadius(b);
        var delta = b.Position - a.Position;
        var dist = delta.Length();
        var minDist = ra + rbRadius;

        if (dist >= minDist || dist <= 0f)
        {
            return;
        }

        var normal = delta / dist;
        var penetration = minDist - dist;
        var invMassA = a.PhysicsType == RigidBodyPhysicsType.FollowBone ? 0f : 1f / MathF.Max(a.Mass, 1e-6f);
        var invMassB = b.PhysicsType == RigidBodyPhysicsType.FollowBone ? 0f : 1f / MathF.Max(b.Mass, 1e-6f);
        var totalInv = invMassA + invMassB;
        if (totalInv > 0f)
        {
            var correction = normal * (penetration / totalInv);
            a.Position -= correction * invMassA;
            b.Position += correction * invMassB;
        }

        var relVel = b.Velocity - a.Velocity;
        var relNormal = Vector3.Dot(relVel, normal);
        if (relNormal < 0f)
        {
            var e = MathF.Min(a.Recoil, b.Recoil);
            var j = -(1f + e) * relNormal / totalInv;
            var impulse = j * normal;
            a.Velocity -= impulse * invMassA;
            b.Velocity += impulse * invMassB;

            var tangentVel = relVel - relNormal * normal;
            if (tangentVel.LengthSquared() > 0f)
            {
                var tangent = Vector3.Normalize(tangentVel);
                var mu = (a.Friction + b.Friction) * 0.5f;
                var jt = -Vector3.Dot(relVel, tangent) * mu / totalInv;
                var frictionImpulse = jt * tangent;
                a.Velocity -= frictionImpulse * invMassA;
                b.Velocity += frictionImpulse * invMassB;
            }
        }
    }

    private static bool ShouldCollide(RigidBody a, RigidBody b)
    {
        var maskA = 1 << b.Group;
        var maskB = 1 << a.Group;
        return (a.GroupTarget & maskA) == 0 && (b.GroupTarget & maskB) == 0;
    }
}
