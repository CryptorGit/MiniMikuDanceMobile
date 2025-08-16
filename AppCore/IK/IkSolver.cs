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
                var jointInfo = constraint.Joints[j];
                int jointIndex = jointInfo.BoneIndex;
                if (jointIndex < 0 || jointIndex >= bones.Count)
                    continue;
                var mats = BuildWorldMatrices(bones);
                var jointPos = mats[jointIndex].Translation;
                var effectorPos = mats[constraint.Effector].Translation;
                var targetPos = mats[constraint.Target].Translation;
                var toEffector = Vector3.Normalize(effectorPos - jointPos);
                var toTarget = Vector3.Normalize(targetPos - jointPos);
                var axis = Vector3.Cross(toEffector, toTarget);
                if (axis.LengthSquared() < 1e-6f)
                    continue;
                axis = Vector3.Normalize(axis);
                float angle = MathF.Acos(Math.Clamp(Vector3.Dot(toEffector, toTarget), -1f, 1f));
                float limit = constraint.AngleLimit * (j + 1);
                if (angle > limit)
                    angle = limit;
                if (jointInfo.HasUnitXConstraint && first)
                    axis = Vector3.UnitX;
                var delta = Quaternion.CreateFromAxisAngle(axis, angle);
                var joint = bones[jointIndex];
                joint.Rotation = Quaternion.Normalize(delta * joint.Rotation);
                joint.Rotation = jointInfo.ApplyLimit(joint.Rotation);
            }
        }
    }

    private static Matrix4x4[] BuildWorldMatrices(IList<BoneData> bones)
    {
        var result = new Matrix4x4[bones.Count];
        for (int i = 0; i < bones.Count; i++)
        {
            var b = bones[i];
            var local = Matrix4x4.CreateFromQuaternion(b.Rotation) * Matrix4x4.CreateTranslation(b.Translation);
            if (b.Parent >= 0 && b.Parent < i)
                result[i] = local * result[b.Parent];
            else
                result[i] = local;
        }
        return result;
    }
}
