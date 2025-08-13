using System.Numerics;
using MiniMikuDance.Data;
using MiniMikuDance.Physix;

namespace MiniMikuDance.IK;

/// <summary>
/// IKチェーンを解決するためのインターフェース。
/// フレーム更新の順序は Animator → 物理 → IK → 最終ボーン行列 に従う。
/// </summary>
public interface IIkSolver
{
    /// <summary>
    /// モデルに含まれる全てのIKを解決する。
    /// 物理演算の結果が反映された後に呼び出すこと。
    /// </summary>
    /// <param name="model">IKを適用するモデル。</param>
    void Solve(MmdModel model);
}

/// <summary>
/// CCDベースのIKソルバ実装。フットIKおよび胸部拡張に対応する。
/// 実際の骨構造・チェーン定義は <see cref="MmdModel"/> 側で保持する想定。
/// </summary>
public class IkSolver : IIkSolver
{
    private readonly IPhysicsEngine _physics;

    public IkSolver(IPhysicsEngine physics)
    {
        _physics = physics;
    }

    /// <inheritdoc />
    public void Solve(MmdModel model)
    {
        // TODO: model.IkChains を走査し SolveCcdChain を適用する
        // TODO: model.FootIkChains を走査し SolveFootIk を適用する
    }

    /// <summary>
    /// CCD法で単一のIKチェーンを解決する。
    /// 到達不能な場合は胸部方向へチェーンを一時拡張して再試行する。
    /// </summary>
    private void SolveCcdChain()
    {
        // TODO: CCDアルゴリズム実装
        // 1. 末端から順にボーンを回転させターゲットに近づける
        // 2. 収束しない場合は親方向へチェーンを拡張し追加反復する
    }

    /// <summary>
    /// フットIKを解決する。
    /// 足首の位置から地面へレイキャストし、ターゲットを補正した上で CCD を適用する。
    /// 必要に応じてルートボーンを上方へ補正する。
    /// </summary>
    private void SolveFootIk()
    {
        // TODO: 足首位置レイキャスト → ターゲット調整 → CCD解 → ルート補正
        // 例:
        // var origin = anklePos + new Vector3(0, 0.5f, 0);
        // if (_physics.Raycast(origin, new Vector3(0, -1, 0), 1.0f, out var hit)) { ... }
    }
}
