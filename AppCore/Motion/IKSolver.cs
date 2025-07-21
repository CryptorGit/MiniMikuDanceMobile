using System.Numerics;
using MiniMikuDance.Import;
using MiniMikuDance.Util;

namespace MiniMikuDance.Motion;

public static class IKSolver
{
    private static Vector3 ExtractTranslation(Matrix4x4 m)
        => new Vector3(m.M41, m.M42, m.M43);

    private static void EnsureCapacity(IList<Vector3> list, int count)
    {
        while (list.Count < count)
            list.Add(Vector3.Zero);
    }

    private static Matrix4x4[] ComputeWorldMatrices(List<BoneData> bones, IList<Vector3> rotations, IList<Vector3> translations)
    {
        var world = new Matrix4x4[bones.Count];
        const float deg2rad = MathF.PI / 180f;
        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            Vector3 euler = i < rotations.Count ? rotations[i] : Vector3.Zero;
            var delta = Quaternion.CreateFromYawPitchRoll(euler.Y * deg2rad, euler.X * deg2rad, euler.Z * deg2rad);
            Vector3 t = bone.Translation;
            if (i < translations.Count) t += translations[i];
            var local = Matrix4x4.CreateFromQuaternion(bone.Rotation * delta) * Matrix4x4.CreateTranslation(t);
            if (bone.Parent >= 0)
                world[i] = local * world[bone.Parent];
            else
                world[i] = local;
        }
        return world;
    }

    private static Quaternion FromToRotation(Vector3 from, Vector3 to)
    {
        from = Vector3.Normalize(from);
        to = Vector3.Normalize(to);
        float dot = Math.Clamp(Vector3.Dot(from, to), -1f, 1f);
        if (dot > 1f - 1e-6f)
            return Quaternion.Identity;
        if (dot < -1f + 1e-6f)
        {
            Vector3 axis = Vector3.Cross(from, Vector3.UnitX);
            if (axis.LengthSquared() < 1e-6f)
                axis = Vector3.Cross(from, Vector3.UnitY);
            axis = Vector3.Normalize(axis);
            return Quaternion.CreateFromAxisAngle(axis, MathF.PI);
        }
        Vector3 axisN = Vector3.Normalize(Vector3.Cross(from, to));
        float angle = MathF.Acos(dot);
        return Quaternion.CreateFromAxisAngle(axisN, angle);
    }

    private static void ApplyRotation(IList<Vector3> rotations, int index, Quaternion delta)
    {
        const float deg2rad = MathF.PI / 180f;
        EnsureCapacity(rotations, index + 1);
        Vector3 euler = rotations[index];
        var current = Quaternion.CreateFromYawPitchRoll(euler.Y * deg2rad, euler.X * deg2rad, euler.Z * deg2rad);
        current = delta * current;
        rotations[index] = current.ToEulerDegrees();
    }

    /// <summary>
    /// Two-bone leg IK solver. Updates rotations in degrees.
    /// </summary>
    public static Vector3 SolveLeg(
        ModelData model,
        IList<Vector3> rotations,
        IList<Vector3> translations,
        string upperLeg,
        string lowerLeg,
        string foot,
        Vector3 target,
        Vector3 pole)
    {
        if (!model.HumanoidBones.TryGetValue(upperLeg, out int upper) ||
            !model.HumanoidBones.TryGetValue(lowerLeg, out int lower) ||
            !model.HumanoidBones.TryGetValue(foot, out int footIdx))
            return Vector3.Zero;

        EnsureCapacity(rotations, model.Bones.Count);
        EnsureCapacity(translations, model.Bones.Count);

        var world = ComputeWorldMatrices(model.Bones, rotations, translations);
        Vector3 hipPos = ExtractTranslation(world[upper]);
        Vector3 kneePos = ExtractTranslation(world[lower]);
        Vector3 footPos = ExtractTranslation(world[footIdx]);

        float lenUpper = (kneePos - hipPos).Length();
        float lenLower = (footPos - kneePos).Length();

        Vector3 rootToTarget = target - hipPos;
        float distTarget = rootToTarget.Length();
        if (distTarget < 1e-6f)
            return footPos;
        distTarget = Math.Clamp(distTarget, MathF.Abs(lenUpper - lenLower) + 1e-4f, lenUpper + lenLower - 1e-4f);

        Vector3 dirTarget = rootToTarget / distTarget;
        Vector3 bendPlaneNormal = Vector3.Normalize(Vector3.Cross(dirTarget, pole - hipPos));
        if (bendPlaneNormal.LengthSquared() < 1e-6f)
            bendPlaneNormal = Vector3.Normalize(Vector3.Cross(dirTarget, Vector3.UnitY));
        Vector3 bendDir = Vector3.Normalize(Vector3.Cross(bendPlaneNormal, dirTarget));

        float cosHip = (lenUpper * lenUpper + distTarget * distTarget - lenLower * lenLower) / (2f * lenUpper * distTarget);
        cosHip = Math.Clamp(cosHip, -1f, 1f);
        float hipAngle = MathF.Acos(cosHip);
        Vector3 newKneePos = hipPos + dirTarget * (MathF.Cos(hipAngle) * lenUpper) + bendDir * (MathF.Sin(hipAngle) * lenUpper);

        Quaternion qHip = FromToRotation(kneePos - hipPos, newKneePos - hipPos);
        Quaternion qKnee = FromToRotation(footPos - kneePos, target - newKneePos);

        ApplyRotation(rotations, upper, qHip);
        ApplyRotation(rotations, lower, qKnee);

        world = ComputeWorldMatrices(model.Bones, rotations, translations);
        return ExtractTranslation(world[footIdx]);
    }

    /// <summary>
    /// Two-bone arm IK solver. Updates rotations in degrees.
    /// </summary>
    public static Vector3 SolveArm(
        ModelData model,
        IList<Vector3> rotations,
        IList<Vector3> translations,
        string upperArm,
        string lowerArm,
        string hand,
        Vector3 target,
        Vector3 pole)
        => SolveLeg(model, rotations, translations, upperArm, lowerArm, hand, target, pole);
}
