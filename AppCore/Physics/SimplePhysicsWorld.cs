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
            var (mass, inertia) = PhysicsHelper.Compute(rb.Shape, rb.Size, rb.Mass);
            RigidBodies.Add(new RigidBody
            {
                Name = rb.Name,
                NameEnglish = rb.NameEnglish,
                BoneIndex = rb.BoneIndex,
                Mass = mass,
                Inertia = inertia,
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

    private static float GetBoundingRadius(RigidBody rb)
    {
        return rb.Shape switch
        {
            RigidBodyShape.Sphere => rb.Size.X * 0.5f,
            RigidBodyShape.Box => rb.Size.Length() * 0.5f,
            RigidBodyShape.Capsule => rb.Size.X * 0.5f + rb.Size.Y * 0.5f,
            _ => MathF.Max(rb.Size.X, MathF.Max(rb.Size.Y, rb.Size.Z)) * 0.5f
        };
    }

    private void ResolveFloorCollision(RigidBody rb)
    {
        if (rb.PhysicsType == RigidBodyPhysicsType.FollowBone)
        {
            return;
        }

        var bottom = rb.Shape == RigidBodyShape.Box
            ? rb.Position.Y - rb.Size.Y * 0.5f
            : rb.Position.Y - GetBoundingRadius(rb);
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

        if (!ComputeCollision(a, b, out var normal, out var penetration))
        {
            return;
        }

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

    private static bool ComputeCollision(RigidBody a, RigidBody b, out Vector3 normal, out float penetration)
    {
        switch (a.Shape, b.Shape)
        {
            case (RigidBodyShape.Sphere, RigidBodyShape.Sphere):
                return SphereSphere(a, b, out normal, out penetration);
            case (RigidBodyShape.Box, RigidBodyShape.Box):
                return BoxBox(a, b, out normal, out penetration);
            case (RigidBodyShape.Sphere, RigidBodyShape.Box):
                return SphereBox(a, b, out normal, out penetration);
            case (RigidBodyShape.Box, RigidBodyShape.Sphere):
                if (SphereBox(b, a, out normal, out penetration))
                {
                    normal = -normal;
                    return true;
                }
                return false;
            default:
                return BoundingSphere(a, b, out normal, out penetration);
        }
    }

    private static bool SphereSphere(RigidBody a, RigidBody b, out Vector3 normal, out float penetration)
    {
        var ra = a.Size.X * 0.5f;
        var rbRadius = b.Size.X * 0.5f;
        var delta = b.Position - a.Position;
        var dist = delta.Length();
        var minDist = ra + rbRadius;
        if (dist >= minDist || dist <= 0f)
        {
            normal = Vector3.UnitY;
            penetration = 0f;
            return false;
        }
        normal = delta / dist;
        penetration = minDist - dist;
        return true;
    }

    private static bool BoxBox(RigidBody a, RigidBody b, out Vector3 normal, out float penetration)
    {
        var halfA = a.Size * 0.5f;
        var halfB = b.Size * 0.5f;
        var minA = a.Position - halfA;
        var maxA = a.Position + halfA;
        var minB = b.Position - halfB;
        var maxB = b.Position + halfB;

        if (maxA.X < minB.X || minA.X > maxB.X ||
            maxA.Y < minB.Y || minA.Y > maxB.Y ||
            maxA.Z < minB.Z || minA.Z > maxB.Z)
        {
            normal = Vector3.UnitY;
            penetration = 0f;
            return false;
        }

        var overlapX = MathF.Min(maxA.X, maxB.X) - MathF.Max(minA.X, minB.X);
        var overlapY = MathF.Min(maxA.Y, maxB.Y) - MathF.Max(minA.Y, minB.Y);
        var overlapZ = MathF.Min(maxA.Z, maxB.Z) - MathF.Max(minA.Z, minB.Z);

        penetration = overlapX;
        normal = new Vector3(MathF.Sign(b.Position.X - a.Position.X), 0f, 0f);

        if (overlapY < penetration)
        {
            penetration = overlapY;
            normal = new Vector3(0f, MathF.Sign(b.Position.Y - a.Position.Y), 0f);
        }
        if (overlapZ < penetration)
        {
            penetration = overlapZ;
            normal = new Vector3(0f, 0f, MathF.Sign(b.Position.Z - a.Position.Z));
        }
        return true;
    }

    private static bool SphereBox(RigidBody sphere, RigidBody box, out Vector3 normal, out float penetration)
    {
        var r = sphere.Size.X * 0.5f;
        var halfB = box.Size * 0.5f;
        var minB = box.Position - halfB;
        var maxB = box.Position + halfB;
        var closest = Vector3.Clamp(sphere.Position, minB, maxB);
        var delta = sphere.Position - closest;
        var dist = delta.Length();
        if (dist >= r || dist <= 0f)
        {
            normal = Vector3.UnitY;
            penetration = 0f;
            return false;
        }
        normal = delta / dist;
        penetration = r - dist;
        return true;
    }

    private static bool BoundingSphere(RigidBody a, RigidBody b, out Vector3 normal, out float penetration)
    {
        var ra = GetBoundingRadius(a);
        var rbRadius = GetBoundingRadius(b);
        var delta = b.Position - a.Position;
        var dist = delta.Length();
        var minDist = ra + rbRadius;
        if (dist >= minDist || dist <= 0f)
        {
            normal = Vector3.UnitY;
            penetration = 0f;
            return false;
        }
        normal = delta / dist;
        penetration = minDist - dist;
        return true;
    }

    private static bool ShouldCollide(RigidBody a, RigidBody b)
    {
        var maskA = 1 << b.Group;
        var maskB = 1 << a.Group;
        return (a.GroupTarget & maskA) == 0 && (b.GroupTarget & maskB) == 0;
    }
}
