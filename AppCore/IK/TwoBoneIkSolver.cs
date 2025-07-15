using System.Collections.Generic;
using System.Numerics;
using MiniMikuDance.Import;

namespace MiniMikuDance.IK;

/// <summary>
/// 2ボーン腕・脚用の簡易IKソルバ。
/// </summary>
public static class TwoBoneIkSolver
{
    /// <summary>
    /// 上腕・下腕(または大腿・脛)の2ボーンでターゲットへ向けます。
    /// </summary>
    /// <param name="target">目標位置</param>
    /// <param name="bones">ルートから順に2つのボーンを渡します</param>
    public static void Solve(Vector3 target, IList<BoneData> bones)
    {
        if (bones.Count != 2) return;
        var chain = new List<BoneData>{ bones[0], bones[1], new BoneData{ Translation = bones[1].Translation } };
        IkSolver.Solve(target, chain);
        bones[0].Rotation = chain[0].Rotation;
        bones[0].Translation = chain[0].Translation;
        bones[1].Rotation = chain[1].Rotation;
        bones[1].Translation = chain[1].Translation;
    }
}
