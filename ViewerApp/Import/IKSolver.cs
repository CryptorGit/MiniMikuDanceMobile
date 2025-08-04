using System;
using System.Collections.Generic;
using System.Numerics;

namespace ViewerApp.Import;

/// <summary>
/// シンプルな CCD アルゴリズムによる IK ソルバ。
/// </summary>
public static class IKSolver
{
    /// <summary>
    /// 指定した終端ボーンが目標位置に到達するようにボーンチェーンを調整します。
    /// </summary>
    /// <param name="bones">ボーンリスト。</param>
    /// <param name="endEffectorIndex">終端ボーンのインデックス。</param>
    /// <param name="target">目標位置。</param>
    /// <param name="iterations">反復回数。</param>
    /// <param name="threshold">収束判定の閾値。</param>
    public static void SolveCCD(IList<BoneData> bones, int endEffectorIndex, Vector3 target, int iterations = 10, float threshold = 1e-3f)
    {
        if (bones == null || bones.Count == 0)
            return;
        if (endEffectorIndex < 0 || endEffectorIndex >= bones.Count)
            return;

        for (int iter = 0; iter < iterations; iter++)
        {
            var world = ComputeWorldTransforms(bones);
            Vector3 endPos = world[endEffectorIndex].Translation;
            if (Vector3.Distance(endPos, target) < threshold)
                break;

            int index = endEffectorIndex;
            while (index >= 0)
            {
                Vector3 jointPos = world[index].Translation;
                Vector3 toEnd = Vector3.Normalize(endPos - jointPos);
                Vector3 toTarget = Vector3.Normalize(target - jointPos);
                float dot = Math.Clamp(Vector3.Dot(toEnd, toTarget), -1f, 1f);
                float angle = MathF.Acos(dot);
                if (angle > 1e-5f)
                {
                    Vector3 axis = Vector3.Normalize(Vector3.Cross(toEnd, toTarget));
                    var rot = Quaternion.CreateFromAxisAngle(axis, angle);
                    bones[index].Rotation = Quaternion.Normalize(rot * bones[index].Rotation);
                    world = ComputeWorldTransforms(bones);
                    endPos = world[endEffectorIndex].Translation;
                }
                index = bones[index].Parent;
            }
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
