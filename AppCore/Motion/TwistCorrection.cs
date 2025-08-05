using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using MiniMikuDance.Import;

namespace MiniMikuDance.Motion;

/// <summary>
/// IK計算後のボーン回転をツイストボーンへ分配する補正クラス。
/// </summary>
public static class TwistCorrection
{
    /// <summary>
    /// 胸や腰の捻りを脊椎ボーンへ均等配分する。
    /// </summary>
    /// <param name="rotations">ボーンの回転リスト。</param>
    /// <param name="indexToHumanoidName">ボーンインデックスとHumanoid名称の対応表。</param>
    public static void DistributeSpineTwist(IList<Vector3> rotations, IDictionary<int, string> indexToHumanoidName)
    {
        if (rotations == null || indexToHumanoidName == null)
            return;

        var nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in indexToHumanoidName)
            nameToIndex[kv.Value] = kv.Key;

        var names = new[]
        {
            HumanoidBones.StandardOrder[0], // hips
            HumanoidBones.StandardOrder[1], // spine
            HumanoidBones.StandardOrder[2]  // chest
        };

        var indices = new List<int>();
        foreach (var n in names)
            if (nameToIndex.TryGetValue(n, out var idx) && idx < rotations.Count)
                indices.Add(idx);

        if (indices.Count <= 1)
            return;

        float total = 0f;
        foreach (var idx in indices)
            total += rotations[idx].Y;
        float per = total / indices.Count;
        foreach (var idx in indices)
        {
            var r = rotations[idx];
            r.Y = per;
            rotations[idx] = r;
        }
    }

    /// <summary>
    /// 腕・脚の捩じりをTwistボーンへ分配する。
    /// </summary>
    /// <param name="rotations">ボーンの回転リスト。</param>
    /// <param name="indexToHumanoidName">ボーンインデックスとHumanoid名称の対応表。</param>
    public static void DistributeLimbTwist(IList<Vector3> rotations, IDictionary<int, string> indexToHumanoidName)
    {
        if (rotations == null || indexToHumanoidName == null)
            return;

        var nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in indexToHumanoidName)
            nameToIndex[kv.Value] = kv.Key;

        var bases = new[]
        {
            HumanoidBones.StandardOrder[5],  // leftUpperArm
            HumanoidBones.StandardOrder[6],  // leftLowerArm
            HumanoidBones.StandardOrder[8],  // rightUpperArm
            HumanoidBones.StandardOrder[9],  // rightLowerArm
            HumanoidBones.StandardOrder[11], // leftUpperLeg
            HumanoidBones.StandardOrder[12], // leftLowerLeg
            HumanoidBones.StandardOrder[14], // rightUpperLeg
            HumanoidBones.StandardOrder[15]  // rightLowerLeg
        };

        foreach (var baseName in bases)
        {
            var twistName = baseName + "Twist";
            if (!nameToIndex.TryGetValue(baseName, out var baseIdx) || baseIdx >= rotations.Count)
                continue;
            if (!nameToIndex.TryGetValue(twistName, out var twistIdx) || twistIdx >= rotations.Count)
                continue;

            float total = rotations[baseIdx].Y + rotations[twistIdx].Y;
            float half = total * 0.5f;
            var baseRot = rotations[baseIdx];
            baseRot.Y = half;
            rotations[baseIdx] = baseRot;
            var twistRot = rotations[twistIdx];
            twistRot.Y = half;
            rotations[twistIdx] = twistRot;
        }
    }
}

