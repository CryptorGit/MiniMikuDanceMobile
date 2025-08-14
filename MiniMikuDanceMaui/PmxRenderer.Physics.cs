using System;
using System.Collections.Generic;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer
{
    private readonly List<IntPtr> _rigidBodies = new();
    private readonly List<IntPtr> _joints = new();
    private DateTime _lastPhysicsUpdate = DateTime.UtcNow;

    private void RegisterPhysics(IList<RigidBodyData> rigidBodies, IList<JointData> joints)
    {
        lock (_physicsLock)
        {
            foreach (var joint in _joints)
            {
                NanoemPhysics.RemoveJoint(joint);
                NanoemPhysics.DestroyJoint(joint);
            }
            _joints.Clear();
            foreach (var body in _rigidBodies)
            {
                NanoemPhysics.RemoveRigidBody(body);
                NanoemPhysics.DestroyRigidBody(body);
            }
            _rigidBodies.Clear();
            foreach (var rb in rigidBodies)
            {
                var bodyPtr = NanoemPhysics.CreateRigidBody(IntPtr.Zero); // TODO: rigid body parameters
                if (bodyPtr != IntPtr.Zero)
                {
                    NanoemPhysics.AddRigidBody(bodyPtr);
                    _rigidBodies.Add(bodyPtr);
                }
            }
            foreach (var jt in joints)
            {
                var jointPtr = NanoemPhysics.CreateJoint(IntPtr.Zero); // TODO: joint parameters
                if (jointPtr != IntPtr.Zero)
                {
                    NanoemPhysics.AddJoint(jointPtr);
                    _joints.Add(jointPtr);
                }
            }
        }
    }

    public void FrameUpdated(float delta)
    {
        // 物理演算は Render で更新するため何もしない
    }

    private void UpdatePhysics(float delta)
    {
        lock (_physicsLock)
        {
            NanoemPhysics.Step(delta);
        }
    }
}
