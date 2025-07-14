using System.Numerics;
using System.Collections.Generic;
using MiniMikuDance.Import;

namespace MiniMikuDance.Motion;

/// <summary>
/// シンプルな2ボーンIKを計算するクラス。
/// </summary>
public class IkSolver
{
    private readonly IList<BoneData> _bones;

    public IkSolver(IList<BoneData> bones)
    {
        _bones = bones;
    }

    /// <summary>
    /// 2ボーンIKを解き、肘や膝などの中間関節の位置を求めます。
    /// </summary>
    /// <param name="rootIndex">ルートボーンのインデックス</param>
    /// <param name="midIndex">中間ボーンのインデックス</param>
    /// <param name="endIndex">終端ボーンのインデックス</param>
    /// <param name="target">終端ボーンが到達すべき位置</param>
    /// <returns>計算された中間ボーンの位置</returns>
    public Vector3 Solve(int rootIndex, int midIndex, int endIndex, Vector3 target)
    {
        if (rootIndex < 0 || midIndex < 0 || endIndex < 0)
            return Vector3.Zero;
        if (rootIndex >= _bones.Count || midIndex >= _bones.Count || endIndex >= _bones.Count)
            return Vector3.Zero;

        Vector3 root = _bones[rootIndex].Translation;
        float len1 = _bones[midIndex].Translation.Length();
        float len2 = _bones[endIndex].Translation.Length();

        Vector3 bend = Vector3.Cross(_bones[midIndex].Translation, _bones[endIndex].Translation);
        if (bend.LengthSquared() < 1e-6f)
            bend = Vector3.UnitY;
        bend = Vector3.Normalize(bend);

        Vector3 dir = target - root;
        float dist = dir.Length();
        if (dist < 1e-6f)
            return root;
        dir /= dist;
        dist = Math.Clamp(dist, 1e-4f, len1 + len2 - 1e-4f);

        float cosA = (len1 * len1 + dist * dist - len2 * len2) / (2f * len1 * dist);
        cosA = Math.Clamp(cosA, -1f, 1f);
        float angle = MathF.Acos(cosA);
        Vector3 axis = Vector3.Normalize(Vector3.Cross(dir, bend));
        if (axis.LengthSquared() < 1e-6f)
            axis = Vector3.UnitY;
        Quaternion rot = Quaternion.CreateFromAxisAngle(axis, angle);
        Vector3 mid = root + Vector3.Transform(dir * len1, rot);
        return mid;
    }
}
