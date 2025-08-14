using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui.Renderers.Pmx;

public partial class PmxRenderer
{
    private readonly List<(IntPtr Body, int BoneIndex)> _rigidBodies = new();
    private readonly List<IntPtr> _joints = new();

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
            foreach (var (body, _) in _rigidBodies)
            {
                NanoemPhysics.RemoveRigidBody(body);
                NanoemPhysics.DestroyRigidBody(body);
            }
            _rigidBodies.Clear();
            foreach (var rb in rigidBodies)
            {
                var config = new NanoemPhysics.RigidBodyConfig
                {
                    Mass = rb.Mass,
                    Shape = (int)rb.Shape,
                    Size = rb.Size,
                    Position = rb.Position,
                    Rotation = rb.Rotation,
                    LinearLower = rb.LinearLower,
                    LinearUpper = rb.LinearUpper,
                    AngularLower = rb.AngularLower,
                    AngularUpper = rb.AngularUpper,
                };
                var bodyPtr = NanoemPhysics.CreateRigidBody(in config);
                if (bodyPtr != IntPtr.Zero)
                {
                    NanoemPhysics.AddRigidBody(bodyPtr);
                    _rigidBodies.Add((bodyPtr, rb.BoneIndex));
                }
            }
            foreach (var jt in joints)
            {
                var config = new NanoemPhysics.JointConfig
                {
                    RigidBodyA = jt.RigidBodyA >= 0 && jt.RigidBodyA < _rigidBodies.Count ? _rigidBodies[jt.RigidBodyA].Body : IntPtr.Zero,
                    RigidBodyB = jt.RigidBodyB >= 0 && jt.RigidBodyB < _rigidBodies.Count ? _rigidBodies[jt.RigidBodyB].Body : IntPtr.Zero,
                    Position = jt.Position,
                    Rotation = jt.Rotation,
                    TranslationLower = jt.TranslationLower,
                    TranslationUpper = jt.TranslationUpper,
                    RotationLower = jt.RotationLower,
                    RotationUpper = jt.RotationUpper,
                    TranslationSpring = jt.TranslationSpring,
                    RotationSpring = jt.RotationSpring,
                };
                var jointPtr = NanoemPhysics.CreateJoint(in config);
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
            foreach (var (body, boneIndex) in _rigidBodies)
            {
                if (boneIndex >= 0 && boneIndex < _worldMats.Length)
                {
                    _worldMats[boneIndex] = NanoemPhysics.GetRigidBodyWorldTransform(body);
                }
            }
        }
    }

    partial void InitializePhysicsModule()
    {
        RegisterModule(new PhysicsModule());
    }

    private class PhysicsModule : PmxRendererModuleBase
    {
    }
}
