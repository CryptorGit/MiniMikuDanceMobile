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
                Position = Vector3.Zero,
                Rotation = Vector3.Zero,
                BoneOffsetPosition = rb.Position,
                BoneOffsetRotation = rb.Rotation,
                Size = rb.Size,
                Group = rb.Group,
                GroupTarget = rb.GroupTarget,
                PhysicsType = rb.PhysicsType
            });
        }
        Joints.Clear();
        foreach (var j in model.Joints)
        {
            var joint = new Joint
            {
                Name = j.Name,
                RigidBodyA = j.RigidBodyA,
                RigidBodyB = j.RigidBodyB,
                Rotation = j.Rotation,
                PositionMin = j.PositionMin,
                PositionMax = j.PositionMax,
                RotationMin = j.RotationMin,
                RotationMax = j.RotationMax,
                SpringPosition = j.SpringPosition,
                SpringRotation = j.SpringRotation
            };
            if (joint.RigidBodyA >= 0 && joint.RigidBodyA < model.RigidBodies.Count)
            {
                var rbA = model.RigidBodies[joint.RigidBodyA];
                var rotA = Matrix4x4.CreateFromYawPitchRoll(rbA.Rotation.Y, rbA.Rotation.X, rbA.Rotation.Z);
                Matrix4x4.Invert(rotA, out var invRotA);
                joint.AnchorA = Vector3.Transform(j.Position - rbA.Position, invRotA);
            }
            if (joint.RigidBodyB >= 0 && joint.RigidBodyB < model.RigidBodies.Count)
            {
                var rbB = model.RigidBodies[joint.RigidBodyB];
                var rotB = Matrix4x4.CreateFromYawPitchRoll(rbB.Rotation.Y, rbB.Rotation.X, rbB.Rotation.Z);
                Matrix4x4.Invert(rotB, out var invRotB);
                joint.AnchorB = Vector3.Transform(j.Position - rbB.Position, invRotB);
            }
            Joints.Add(joint);
            if (joint.RigidBodyA >= 0 && joint.RigidBodyA < RigidBodies.Count)
            {
                RigidBodies[joint.RigidBodyA].Joints.Add(joint);
            }
            if (joint.RigidBodyB >= 0 && joint.RigidBodyB < RigidBodies.Count)
            {
                RigidBodies[joint.RigidBodyB].Joints.Add(joint);
            }
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

        foreach (var j in Joints)
        {
            SolveJoint(j, dt);
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

            var boneWorld = GetBoneWorldMatrix(scene, rb.BoneIndex);
            var offset = Matrix4x4.CreateFromYawPitchRoll(rb.BoneOffsetRotation.Y, rb.BoneOffsetRotation.X, rb.BoneOffsetRotation.Z) *
                         Matrix4x4.CreateTranslation(rb.BoneOffsetPosition);
            var world = offset * boneWorld;
            rb.Position = world.Translation;
            rb.Rotation = ToEuler(Quaternion.CreateFromRotationMatrix(world));
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

            var world = Matrix4x4.CreateFromYawPitchRoll(rb.Rotation.Y, rb.Rotation.X, rb.Rotation.Z) *
                        Matrix4x4.CreateTranslation(rb.Position);
            var offset = Matrix4x4.CreateFromYawPitchRoll(rb.BoneOffsetRotation.Y, rb.BoneOffsetRotation.X, rb.BoneOffsetRotation.Z) *
                         Matrix4x4.CreateTranslation(rb.BoneOffsetPosition);
            Matrix4x4.Invert(offset, out var invOffset);
            var boneWorld = invOffset * world;

            var bone = scene.Bones[rb.BoneIndex];
            if (bone.Parent >= 0 && bone.Parent < scene.Bones.Count)
            {
                var parentWorld = GetBoneWorldMatrix(scene, bone.Parent);
                Matrix4x4.Invert(parentWorld, out var invParent);
                var local = boneWorld * invParent;
                bone.Translation = local.Translation;
                bone.Rotation = Quaternion.CreateFromRotationMatrix(local);
            }
            else
            {
                bone.Translation = boneWorld.Translation;
                bone.Rotation = Quaternion.CreateFromRotationMatrix(boneWorld);
            }
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
            var pos = rb.Position;
            pos.Y -= bottom;
            rb.Position = pos;
            var vn = rb.Velocity.Y;
            if (vn < 0f)
            {
                var vel = rb.Velocity;
                vel.Y = -vn * rb.Recoil;
                var tangent = new Vector3(vel.X, 0f, vel.Z);
                vel -= tangent * rb.Friction;
                rb.Velocity = vel;
            }
        }
    }

    private void SolveJoint(Joint joint, float dt)
    {
        if (joint.RigidBodyA < 0 || joint.RigidBodyA >= RigidBodies.Count || joint.RigidBodyB < 0 || joint.RigidBodyB >= RigidBodies.Count)
        {
            return;
        }

        var a = RigidBodies[joint.RigidBodyA];
        var b = RigidBodies[joint.RigidBodyB];
        var current = (a.Position + joint.AnchorA) - (b.Position + joint.AnchorB);
        var error = current;
        var relVel = a.Velocity - b.Velocity;
        var force = error * joint.SpringPosition - relVel * joint.SpringPosition;
        if (a.PhysicsType != RigidBodyPhysicsType.FollowBone && a.Mass > 0f)
        {
            a.Velocity -= force / a.Mass * dt;
        }
        if (b.PhysicsType != RigidBodyPhysicsType.FollowBone && b.Mass > 0f)
        {
            b.Velocity += force / b.Mass * dt;
        }

        var rotError = (b.Rotation - a.Rotation) - joint.Rotation;
        var relAng = b.AngularVelocity - a.AngularVelocity;
        var torque = rotError * joint.SpringRotation - relAng * joint.SpringRotation;

        if (a.PhysicsType != RigidBodyPhysicsType.FollowBone)
        {
            var invIa = new Vector3(
                a.Inertia.X > 0f ? 1f / a.Inertia.X : 0f,
                a.Inertia.Y > 0f ? 1f / a.Inertia.Y : 0f,
                a.Inertia.Z > 0f ? 1f / a.Inertia.Z : 0f);
            a.AngularVelocity += torque * invIa * dt;
        }
        if (b.PhysicsType != RigidBodyPhysicsType.FollowBone)
        {
            var invIb = new Vector3(
                b.Inertia.X > 0f ? 1f / b.Inertia.X : 0f,
                b.Inertia.Y > 0f ? 1f / b.Inertia.Y : 0f,
                b.Inertia.Z > 0f ? 1f / b.Inertia.Z : 0f);
            b.AngularVelocity -= torque * invIb * dt;
        }
    }

    private static Matrix4x4 GetBoneWorldMatrix(Scene scene, int index)
    {
        var mat = Matrix4x4.Identity;
        var current = index;
        while (current >= 0 && current < scene.Bones.Count)
        {
            var b = scene.Bones[current];
            var local = Matrix4x4.CreateFromQuaternion(b.Rotation) * Matrix4x4.CreateTranslation(b.Translation);
            mat = local * mat;
            current = b.Parent;
        }
        return mat;
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
