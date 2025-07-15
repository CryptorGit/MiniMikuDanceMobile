using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.IK;

public static class IkSolver
{
    /// <summary>
    /// FABRIK方式でボーンチェーンをターゲットへ向けます。
    /// </summary>
    /// <param name="target">最終到達点</param>
    /// <param name="bones">ルートからエンドまで順に並べたボーンリスト</param>
    /// <param name="iteration">繰り返し回数</param>
    public static void Solve(Vector3 target, IList<BoneData> bones, int iteration = 10)
    {
        if (bones.Count < 2) return;

        var positions = new Vector3[bones.Count];
        for (int i = 0; i < bones.Count; i++)
            positions[i] = bones[i].Translation;

        var lengths = new float[bones.Count - 1];
        float total = 0;
        for (int i = 0; i < lengths.Length; i++)
        {
            lengths[i] = Vector3.Distance(positions[i], positions[i + 1]);
            total += lengths[i];
        }

        Vector3 root = positions[0];
        if (Vector3.Distance(root, target) >= total)
        {
            // 目標に届かない場合は一直線に向ける
            var dir = Vector3.Normalize(target - root);
            for (int i = 1; i < positions.Length; i++)
                positions[i] = positions[i - 1] + dir * lengths[i - 1];
        }
        else
        {
            for (int it = 0; it < iteration; it++)
            {
                // forward
                positions[^1] = target;
                for (int i = bones.Count - 2; i >= 0; i--)
                {
                    var dir = Vector3.Normalize(positions[i] - positions[i + 1]);
                    positions[i] = positions[i + 1] + dir * lengths[i];
                }
                // backward
                positions[0] = root;
                for (int i = 1; i < bones.Count; i++)
                {
                    var dir = Vector3.Normalize(positions[i] - positions[i - 1]);
                    positions[i] = positions[i - 1] + dir * lengths[i - 1];
                }
            }
        }

        // 位置を更新
        for (int i = 0; i < bones.Count; i++)
            bones[i].Translation = positions[i];

        // 回転を算出 (Y軸をボーン方向と仮定)
        for (int i = 0; i < bones.Count - 1; i++)
        {
            var dir = Vector3.Normalize(positions[i + 1] - positions[i]);
            if (dir.LengthSquared() < 1e-6f)
            {
                bones[i].Rotation = Quaternion.Identity;
                continue;
            }
            var axis = Vector3.Cross(Vector3.UnitY, dir);
            float len = axis.Length();
            if (len < 1e-6f)
            {
                bones[i].Rotation = Quaternion.Identity;
            }
            else
            {
                axis /= len;
                float angle = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.UnitY, dir), -1f, 1f));
                bones[i].Rotation = Quaternion.CreateFromAxisAngle(axis, angle);
            }
        }
    }
}
