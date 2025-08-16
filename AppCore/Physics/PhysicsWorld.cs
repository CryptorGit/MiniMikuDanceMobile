namespace MiniMikuDance.Physics;

using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;

/// <summary>
/// nanoem 物理シミュレーション世界。
/// </summary>
public sealed class PhysicsWorld : IDisposable
{
    private readonly List<RigidBody> _rigidBodies = new();
    private readonly List<Joint> _joints = new();
    private readonly nint _handle;

    public Vector3 Gravity { get; set; } = new(0f, 0f, -9.8f);

    public Action<RigidBody>? BoneUpdateHook { get; set; }

    public IReadOnlyList<RigidBody> RigidBodies => _rigidBodies;
    public IReadOnlyList<Joint> Joints => _joints;

    public PhysicsWorld()
    {
        _handle = NanoemPhysicsNative.PhysicsWorldCreate(nint.Zero, out var status);
        NanoemPhysicsNative.ThrowIfError(status);
    }

    public RigidBody CreateRigidBody(RigidBodyData data)
    {
        var body = new RigidBody(data.Name, data.BoneIndex, data.TransformType, nint.Zero);
        _rigidBodies.Add(body);
        if (body.Handle != nint.Zero)
            NanoemPhysicsNative.PhysicsWorldAddRigidBody(_handle, body.Handle);
        return body;
    }

    public Joint CreateJoint(JointData data)
    {
        var bodyA = data.RigidBodyA >= 0 && data.RigidBodyA < _rigidBodies.Count ? _rigidBodies[data.RigidBodyA] : null;
        var bodyB = data.RigidBodyB >= 0 && data.RigidBodyB < _rigidBodies.Count ? _rigidBodies[data.RigidBodyB] : null;
        var joint = new Joint(data.Name, bodyA, bodyB, nint.Zero);
        _joints.Add(joint);
        if (joint.Handle != nint.Zero)
            NanoemPhysicsNative.PhysicsWorldAddJoint(_handle, joint.Handle);
        return joint;
    }

    public void Step(float deltaTime)
    {
        NanoemPhysicsNative.PhysicsWorldSetGravity(_handle, new[] { Gravity.X, Gravity.Y, Gravity.Z });
        NanoemPhysicsNative.PhysicsWorldStepSimulation(_handle, deltaTime);

        foreach (var body in _rigidBodies)
        {
            if (body.Handle == nint.Zero)
                continue;
            var matrix = new float[16];
            NanoemPhysicsNative.PhysicsRigidBodyGetWorldTransform(body.Handle, matrix);
            PhysicsUtil.ExtractTransform(matrix, out var translation, out var rotation);
            body.Position = translation;
            body.Orientation = rotation;
        }

        if (BoneUpdateHook != null)
        {
            foreach (var body in _rigidBodies)
            {
                if (body.TransformType != RigidBodyTransformType.FromBoneToSimulation)
                    BoneUpdateHook(body);
            }
        }
    }

    public void Dispose()
    {
        foreach (var joint in _joints)
            joint.Dispose();
        foreach (var body in _rigidBodies)
            body.Dispose();
        NanoemPhysicsNative.PhysicsWorldDestroy(_handle);
    }
}

