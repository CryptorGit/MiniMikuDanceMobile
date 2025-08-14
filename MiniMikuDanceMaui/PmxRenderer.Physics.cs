using System;
using System.Collections.Generic;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui;

public partial class PmxRenderer
{
    private readonly List<IntPtr> _rigidBodies = new();

    private void RegisterRigidBodies(IList<RigidBodyData> rigidBodies)
    {
        lock (_physicsLock)
        {
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

    public void FrameUpdated(float delta)
    {
        lock (_physicsLock)
        {
            NanoemPhysics.Step(delta);
        }
    }
}
