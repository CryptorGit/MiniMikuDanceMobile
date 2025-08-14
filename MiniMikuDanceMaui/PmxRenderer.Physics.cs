using System;
using System.Collections.Generic;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer
{
    private readonly List<IntPtr> _rigidBodies = new();
    private readonly List<IntPtr> _joints = new();

    private void RegisterRigidBodies(IList<RigidBodyData> rigidBodies)
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
        }
    }

    private void RegisterJoints(IList<JointData> joints)
    {
        lock (_physicsLock)
        {
            foreach (var joint in _joints)
            {
                NanoemPhysics.RemoveJoint(joint);
                NanoemPhysics.DestroyJoint(joint);
            }
            _joints.Clear();
            foreach (var jd in joints)
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
        lock (_physicsLock)
        {
            NanoemPhysics.Step(delta);
        }
    }
}
