using System;
using System.Collections.Generic;
using System.Numerics;

namespace ViewerApp.Import;

/// <summary>
/// 2ボーン解析解によるIKソルバ。
/// </summary>
public static class AnalyticTwoBoneSolver
{
    /// <summary>
    /// 2ボーンチェーンを解析的に解きます。
    /// </summary>
    /// <param name="bones">ボーンリスト。</param>
    /// <param name="rootIndex">起点ボーンのインデックス。</param>
    /// <param name="midIndex">中間ボーンのインデックス。</param>
    /// <param name="endIndex">終端ボーンのインデックス。</param>
    /// <param name="target">目標位置。</param>
    public static void Solve(IList<BoneData> bones, int rootIndex, int midIndex, int endIndex, Vector3 target)
    {
        if (bones == null || bones.Count == 0)
            return;

        var world = ComputeWorldTransforms(bones);
        Vector3 rootPos = world[rootIndex].Translation;
        Vector3 midPos = world[midIndex].Translation;
        Vector3 endPos = world[endIndex].Translation;

        float len1 = Vector3.Distance(rootPos, midPos);
        float len2 = Vector3.Distance(midPos, endPos);

        Vector3 targetDir = target - rootPos;
        float targetDist = targetDir.Length();
        targetDir = targetDist > 1e-6f ? targetDir / targetDist : Vector3.UnitZ;
        targetDist = Math.Clamp(targetDist, MathF.Abs(len1 - len2) + 1e-5f, len1 + len2 - 1e-5f);

        float angle0 = MathF.Acos(Math.Clamp((len1 * len1 + targetDist * targetDist - len2 * len2) / (2f * len1 * targetDist), -1f, 1f));
        float angle1 = MathF.Acos(Math.Clamp((len1 * len1 + len2 * len2 - targetDist * targetDist) / (2f * len1 * len2), -1f, 1f));

        Vector3 currentDir = Vector3.Normalize(endPos - rootPos);
        float dot = Math.Clamp(Vector3.Dot(currentDir, targetDir), -1f, 1f);
        if (dot < 0.9999f)
        {
            Vector3 axis = Vector3.Normalize(Vector3.Cross(currentDir, targetDir));
            float ang = MathF.Acos(dot);
            var rot = Quaternion.CreateFromAxisAngle(axis, ang);
            bones[rootIndex].Rotation = Quaternion.Normalize(rot * bones[rootIndex].Rotation);
            world = ComputeWorldTransforms(bones);
            rootPos = world[rootIndex].Translation;
            midPos = world[midIndex].Translation;
            endPos = world[endIndex].Translation;
        }

        Vector3 bendAxis = Vector3.Normalize(Vector3.Cross(midPos - rootPos, endPos - midPos));
        if (bendAxis.LengthSquared() < 1e-8f)
            bendAxis = Vector3.UnitY;

        Vector3 toMid = Vector3.Normalize(midPos - rootPos);
        Vector3 toTarget = Vector3.Normalize(target - rootPos);
        float currentRootAngle = MathF.Acos(Math.Clamp(Vector3.Dot(toMid, toTarget), -1f, 1f));
        float deltaRoot = angle0 - currentRootAngle;
        if (MathF.Abs(deltaRoot) > 1e-5f)
        {
            var rot = Quaternion.CreateFromAxisAngle(bendAxis, deltaRoot);
            bones[rootIndex].Rotation = Quaternion.Normalize(rot * bones[rootIndex].Rotation);
        }

        world = ComputeWorldTransforms(bones);
        rootPos = world[rootIndex].Translation;
        midPos = world[midIndex].Translation;
        endPos = world[endIndex].Translation;

        toMid = Vector3.Normalize(midPos - rootPos);
        Vector3 toEnd = Vector3.Normalize(endPos - midPos);
        float currentMidAngle = MathF.Acos(Math.Clamp(Vector3.Dot(toMid, toEnd), -1f, 1f));
        float desiredMidAngle = MathF.PI - angle1;
        float deltaMid = desiredMidAngle - currentMidAngle;
        if (MathF.Abs(deltaMid) > 1e-5f)
        {
            var rot = Quaternion.CreateFromAxisAngle(bendAxis, deltaMid);
            bones[midIndex].Rotation = Quaternion.Normalize(rot * bones[midIndex].Rotation);
        }
    }

    private static Matrix4x4[] ComputeWorldTransforms(IList<BoneData> bones)
    {
        var world = new Matrix4x4[bones.Count];
        for (int i = 0; i < bones.Count; i++)
        {
            Matrix4x4 local = Matrix4x4.CreateFromQuaternion(bones[i].Rotation) * Matrix4x4.CreateTranslation(bones[i].Translation);
            if (bones[i].Parent >= 0 && bones[i].Parent < i)
            {
                world[i] = local * world[bones[i].Parent];
            }
            else
            {
                world[i] = local;
            }
        }
        return world;
    }
}

