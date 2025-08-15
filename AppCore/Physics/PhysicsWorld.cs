namespace MiniMikuDance.Physics;

using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;

/// <summary>
/// 物理シミュレーション世界。
/// </summary>
public class PhysicsWorld
{
    private readonly List<RigidBody> _rigidBodies = new();
    private readonly List<Joint> _joints = new();

    public Vector3 Gravity { get; set; } = new(0f, 0f, -9.8f);

    public IReadOnlyList<RigidBody> RigidBodies => _rigidBodies;
    public IReadOnlyList<Joint> Joints => _joints;

    public RigidBody CreateRigidBody(RigidBodyData data)
    {
        var body = new RigidBody(
            data.Name,
            data.BoneIndex,
            data.Mass,
            data.Shape,
            data.Size,
            data.Origin,
            data.Orientation,
            data.LinearDamping,
            data.AngularDamping,
            data.Restitution,
            data.Friction,
            data.TransformType,
            data.IsBoneRelative);
        _rigidBodies.Add(body);
        return body;
    }

    public Joint CreateJoint(JointData data)
    {
        var bodyA = data.RigidBodyA >= 0 && data.RigidBodyA < _rigidBodies.Count
            ? _rigidBodies[data.RigidBodyA]
            : null;
        var bodyB = data.RigidBodyB >= 0 && data.RigidBodyB < _rigidBodies.Count
            ? _rigidBodies[data.RigidBodyB]
            : null;
        var joint = new Joint(data.Name, bodyA, bodyB);
        _joints.Add(joint);
        return joint;
    }

    public void Step(float deltaTime)
    {
        foreach (var body in _rigidBodies)
        {
            if (body.TransformType == RigidBodyTransformType.FromBoneToSimulation)
            {
                body.ApplyGravity(Gravity, deltaTime);
            }
            body.Integrate(deltaTime);
        }
        foreach (var joint in _joints)
        {
            joint.Solve();
        }
    }
}
