using System;
using System.Collections.Generic;
using BulletSharp;
using MiniMikuDance.Import;
using BVector3 = BulletSharp.Math.Vector3;
using BMatrix = BulletSharp.Math.Matrix;

namespace MiniMikuDance.Physics;

public class PhysicsManager : IDisposable
{
    private readonly DefaultCollisionConfiguration _config;
    private readonly CollisionDispatcher _dispatcher;
    private readonly BroadphaseInterface _broadphase;
    private readonly DiscreteDynamicsWorld _world;
    private readonly List<RigidBody> _bodies = new();
    private readonly List<TypedConstraint> _constraints = new();

    public DiscreteDynamicsWorld World => _world;

    public PhysicsManager()
    {
        _config = new DefaultCollisionConfiguration();
        _dispatcher = new CollisionDispatcher(_config);
        _broadphase = new DbvtBroadphase();
        _world = new DiscreteDynamicsWorld(_dispatcher, _broadphase, null, _config)
        {
            Gravity = new BVector3(0, -9.81f, 0)
        };
    }

    public RigidBody CreateRigidBody(RigidBodyData data)
    {
        CollisionShape shape = data.Shape switch
        {
            RigidBodyShape.Sphere => new SphereShape(data.Size.X),
            RigidBodyShape.Box => new BoxShape(data.Size.X * 0.5f, data.Size.Y * 0.5f, data.Size.Z * 0.5f),
            RigidBodyShape.Capsule => new CapsuleShape(data.Size.X, data.Size.Y),
            _ => new BoxShape(0.5f)
        };

        var transform = BMatrix.RotationYawPitchRoll(data.Rotation.Y, data.Rotation.X, data.Rotation.Z);
        transform.M41 = data.Position.X;
        transform.M42 = data.Position.Y;
        transform.M43 = data.Position.Z;
        var motion = new DefaultMotionState(transform);
        float mass = data.PhysicsType == RigidBodyPhysicsType.Static ? 0f : data.Mass;
        BVector3 inertia = BVector3.Zero;
        if (mass > 0)
        {
            shape.CalculateLocalInertia(mass, out inertia);
        }
        var info = new RigidBodyConstructionInfo(mass, motion, shape, inertia)
        {
            LinearDamping = data.LinearDamping,
            AngularDamping = data.AngularDamping,
            Restitution = data.Restitution,
            Friction = data.Friction
        };
        var body = new RigidBody(info)
        {
            UserObject = data
        };
        switch (data.PhysicsType)
        {
            case RigidBodyPhysicsType.Static:
                body.CollisionFlags |= CollisionFlags.StaticObject;
                body.ActivationState = ActivationState.DisableSimulation;
                break;
            case RigidBodyPhysicsType.DynamicAndBonePosition:
                body.ActivationState = ActivationState.DisableDeactivation;
                break;
            default:
                body.ActivationState = ActivationState.ActiveTag;
                break;
        }
        _world.AddRigidBody(body, (CollisionFilterGroups)data.CollisionGroup, (CollisionFilterGroups)data.CollisionMask);
        _bodies.Add(body);
        return body;
    }

    public TypedConstraint CreateConstraint(JointData joint, RigidBody bodyA, RigidBody bodyB)
    {
        var frameA = BMatrix.Identity;
        var frameB = BMatrix.Identity;
        var constraint = new Generic6DofSpringConstraint(bodyA, bodyB, frameA, frameB, true)
        {
            LinearLowerLimit = joint.LinearLowerLimit.ToBullet(),
            LinearUpperLimit = joint.LinearUpperLimit.ToBullet(),
            AngularLowerLimit = joint.AngularLowerLimit.ToBullet(),
            AngularUpperLimit = joint.AngularUpperLimit.ToBullet()
        };
        constraint.SetStiffness(0, joint.SpringTranslation.X);
        constraint.SetStiffness(1, joint.SpringTranslation.Y);
        constraint.SetStiffness(2, joint.SpringTranslation.Z);
        constraint.SetStiffness(3, joint.SpringRotation.X);
        constraint.SetStiffness(4, joint.SpringRotation.Y);
        constraint.SetStiffness(5, joint.SpringRotation.Z);
        for (int i = 0; i < 6; i++)
            constraint.EnableSpring(i, true);
        _world.AddConstraint(constraint, true);
        _constraints.Add(constraint);
        return constraint;
    }

    public void StepSimulation(float timeStep)
    {
        _world.StepSimulation(timeStep);
    }

    public void Dispose()
    {
        foreach (var c in _constraints)
        {
            _world.RemoveConstraint(c);
            c.Dispose();
        }
        foreach (var b in _bodies)
        {
            _world.RemoveRigidBody(b);
            b.MotionState?.Dispose();
            b.Dispose();
        }
        _world.Dispose();
        _broadphase.Dispose();
        _dispatcher.Dispose();
        _config.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal static class BulletExtensions
{
    public static BulletSharp.Math.Vector3 ToBullet(this System.Numerics.Vector3 v)
    {
        return new BulletSharp.Math.Vector3(v.X, v.Y, v.Z);
    }
}
