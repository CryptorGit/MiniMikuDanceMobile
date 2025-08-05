using System;
using System.Collections.Generic;
using System.Numerics;

namespace ViewerApp.Import;

/// <summary>
/// FABRIK アルゴリズムによるIKソルバ。
/// </summary>
public static class FabrikSolver
{
    /// <summary>
    /// ボーンチェーンをFABRIKで解きます。
    /// </summary>
    /// <param name="bones">ボーンリスト。</param>
    /// <param name="chain">起点から終端までのボーンインデックス列。</param>
    /// <param name="target">目標位置。</param>
    /// <param name="iterations">反復回数。</param>
    /// <param name="threshold">収束判定の閾値。</param>
    public static void Solve(IList<BoneData> bones, IList<int> chain, Vector3 target, int iterations = 10, float threshold = 1e-3f)
    {
        if (bones == null || chain == null || chain.Count == 0)
            return;

        var world = ComputeWorldTransforms(bones);
        int n = chain.Count;
        var positions = new Vector3[n];
        for (int i = 0; i < n; i++)
            positions[i] = world[chain[i]].Translation;

        var lengths = new float[n - 1];
        float total = 0f;
        for (int i = 0; i < n - 1; i++)
        {
            float len = Vector3.Distance(positions[i + 1], positions[i]);
            lengths[i] = len;
            total += len;
        }

        Vector3 rootPos = positions[0];
        if (Vector3.Distance(rootPos, target) > total)
        {
            Vector3 dir = Vector3.Normalize(target - rootPos);
            for (int i = 1; i < n; i++)
            {
                positions[i] = positions[i - 1] + dir * lengths[i - 1];
            }
        }
        else
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                positions[n - 1] = target;
                for (int i = n - 2; i >= 0; i--)
                {
                    Vector3 dir = Vector3.Normalize(positions[i] - positions[i + 1]);
                    positions[i] = positions[i + 1] + dir * lengths[i];
                }

                positions[0] = rootPos;
                for (int i = 0; i < n - 1; i++)
                {
                    Vector3 dir = Vector3.Normalize(positions[i + 1] - positions[i]);
                    positions[i + 1] = positions[i] + dir * lengths[i];
                }

                if (Vector3.Distance(positions[n - 1], target) < threshold)
                    break;
            }
        }

        world = ComputeWorldTransforms(bones);
        for (int i = 0; i < n - 1; i++)
        {
            int idx = chain[i];
            Vector3 currentDir = Vector3.Normalize(world[chain[i + 1]].Translation - world[idx].Translation);
            Vector3 newDir = Vector3.Normalize(positions[i + 1] - positions[i]);
            float dot = Math.Clamp(Vector3.Dot(currentDir, newDir), -1f, 1f);
            if (dot < 0.9999f)
            {
                Vector3 axis = Vector3.Normalize(Vector3.Cross(currentDir, newDir));
                float angle = MathF.Acos(dot);
                var rot = Quaternion.CreateFromAxisAngle(axis, angle);
                bones[idx].Rotation = Quaternion.Normalize(rot * bones[idx].Rotation);
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

