using System;
using System.Numerics;

namespace MiniMikuDance.Import;

public partial class ModelImporter
{
    partial void LoadPhysics(IntPtr model, ModelData data)
    {
        uint rigidBodyCount = Nanoem.ModelGetRigidBodyCount(model);
        for (uint i = 0; i < rigidBodyCount; i++)
        {
            var rb = Nanoem.ModelGetRigidBodyInfo(model, i);
            data.RigidBodies.Add(new RigidBodyData
            {
                Name = Nanoem.PtrToStringAndFree(rb.Name),
                BoneIndex = rb.BoneIndex,
                Mass = rb.Mass,
                Shape = (RigidBodyShape)rb.ShapeType,
                Position = new Vector3(rb.OriginX * Scale, rb.OriginY * Scale, rb.OriginZ * Scale),
                Rotation = new Vector3(rb.OrientationX, rb.OrientationY, rb.OrientationZ),
                Size = new Vector3(rb.SizeX * Scale, rb.SizeY * Scale, rb.SizeZ * Scale),
                LinearDamping = rb.LinearDamping,
                AngularDamping = rb.AngularDamping,
                Restitution = rb.Restitution,
                Friction = rb.Friction,
                Group = rb.Group,
                Mask = rb.Mask,
                TransformType = rb.TransformType
            });
        }
        uint jointCount = Nanoem.ModelGetJointCount(model);
        for (uint i = 0; i < jointCount; i++)
        {
            var j = Nanoem.ModelGetJointInfo(model, i);
            data.Joints.Add(new JointData
            {
                Name = Nanoem.PtrToStringAndFree(j.Name),
                RigidBodyA = j.RigidBodyA,
                RigidBodyB = j.RigidBodyB,
                Position = new Vector3(j.OriginX * Scale, j.OriginY * Scale, j.OriginZ * Scale),
                Rotation = new Vector3(j.OrientationX, j.OrientationY, j.OrientationZ),
                LinearLowerLimit = new Vector3(j.LinearLowerLimitX, j.LinearLowerLimitY, j.LinearLowerLimitZ),
                LinearUpperLimit = new Vector3(j.LinearUpperLimitX, j.LinearUpperLimitY, j.LinearUpperLimitZ),
                AngularLowerLimit = new Vector3(j.AngularLowerLimitX, j.AngularLowerLimitY, j.AngularLowerLimitZ),
                AngularUpperLimit = new Vector3(j.AngularUpperLimitX, j.AngularUpperLimitY, j.AngularUpperLimitZ),
                LinearStiffness = new Vector3(j.LinearStiffnessX, j.LinearStiffnessY, j.LinearStiffnessZ),
                AngularStiffness = new Vector3(j.AngularStiffnessX, j.AngularStiffnessY, j.AngularStiffnessZ)
            });
        }
    }
}
