using System;
using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.IK;

public static class IkSolver
{
    public static void SolveConstraint(Constraint constraint, IList<BoneData> bones)
    {
        if (constraint.Joints.Count == 0)
            return;
        for (int i = 0; i < constraint.Iterations; i++)
        {
            bool first = i == 0;
            for (int j = 0; j < constraint.Joints.Count; j++)
            {
                var joint = constraint.Joints[j];
                var effectorWorld = GetWorldMatrix(constraint.Effector, bones).Translation;
                var targetWorld = GetWorldMatrix(constraint.Target, bones).Translation;
                var jointWorld = GetWorldMatrix(joint.BoneIndex, bones).Translation;

                var toEffector = Vector3.Normalize(effectorWorld - jointWorld);
                var toTarget = Vector3.Normalize(targetWorld - jointWorld);
                float dot = Math.Clamp(Vector3.Dot(toEffector, toTarget), -1f, 1f);
                float angle = MathF.Acos(dot);
                if (angle < 1e-5f)
                    continue;
                var axis = Vector3.Normalize(Vector3.Cross(toEffector, toTarget));
                if (first)
                {
                    if (joint.HasUnitXConstraint) axis = Vector3.UnitX;
                    else if (joint.HasUnitYConstraint) axis = Vector3.UnitY;
                    else if (joint.HasUnitZConstraint) axis = Vector3.UnitZ;
                }
                float limit = constraint.AngleLimit * (j + 1);
                angle = MathF.Min(angle, limit);
                var rot = Quaternion.CreateFromAxisAngle(axis, angle);
                var current = bones[joint.BoneIndex].Rotation;
                var mixed = Quaternion.Normalize(rot * current);
                mixed = joint.Clamp(mixed);
                bones[joint.BoneIndex].Rotation = mixed;
            }
        }
    }

    private static Matrix4x4 GetWorldMatrix(int index, IList<BoneData> bones)
    {
        var bone = bones[index];
        var local = Matrix4x4.CreateFromQuaternion(bone.Rotation) * Matrix4x4.CreateTranslation(bone.Translation);
        if (bone.Parent >= 0 && bone.Parent < bones.Count)
        {
            return local * GetWorldMatrix(bone.Parent, bones);
        }
        return local;
    }
}
