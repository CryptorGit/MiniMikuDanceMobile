using System;
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
        foreach (var chain in model.IkChains)
            SolveCcdChain(chain);

        foreach (var chain in model.FootIkChains)
            SolveFootIk(model, chain);
    }

    /// <summary>
    /// CCD法で単一のIKチェーンを解決する。
    /// 到達不能な場合は胸部方向へチェーンを一時拡張して再試行する。
    /// </summary>
    private void SolveCcdChain(IkChain chain)
    {
        var bones = chain.Bones;
        if (bones.Length == 0)
            return;

        var target = chain.Target;
        float thresholdSq = chain.Threshold * chain.Threshold;

        for (int iter = 0; iter < chain.MaxIterations; iter++)
        {
            var endEffector = bones[^1];
            if (Vector3.DistanceSquared(endEffector.Position, target) <= thresholdSq)
                return;

            for (int i = bones.Length - 2; i >= 0; i--)
            {
                var joint = bones[i];
                var toEnd = bones[^1].Position - joint.Position;
                var toTarget = target - joint.Position;

                if (toEnd.LengthSquared() < 1e-8f || toTarget.LengthSquared() < 1e-8f)
                    continue;

                toEnd = Vector3.Normalize(toEnd);
                toTarget = Vector3.Normalize(toTarget);

                var axis = Vector3.Cross(toEnd, toTarget);
                if (axis.LengthSquared() < 1e-8f)
                    continue;
                axis = Vector3.Normalize(axis);

                var dot = Vector3.Dot(toEnd, toTarget);
                dot = Math.Clamp(dot, -1f, 1f);
                var angle = MathF.Acos(dot);
                if (joint.RotationLimit > 0)
                    angle = MathF.Min(angle, joint.RotationLimit);

                var rot = Quaternion.CreateFromAxisAngle(axis, angle);
                joint.Rotation = Quaternion.Normalize(rot * joint.Rotation);

                for (int j = i + 1; j < bones.Length; j++)
                {
                    var child = bones[j];
                    var rel = child.Position - joint.Position;
                    rel = Vector3.Transform(rel, rot);
                    child.Position = joint.Position + rel;
                }
            }
        }

        if (Vector3.DistanceSquared(bones[^1].Position, target) > thresholdSq && chain.ChestBone != null)
        {
            var extended = new IkChain(
                new[] { chain.ChestBone }.Concat(bones).ToArray(),
                target)
            {
                MaxIterations = chain.MaxIterations,
                Threshold = chain.Threshold
            };
            SolveCcdChain(extended);
        }
    }

    /// <summary>
    /// フットIKを解決する。
    /// 足首の位置から地面へレイキャストし、ターゲットを補正した上で CCD を適用する。
    /// 必要に応じてルートボーンを上方へ補正する。
    /// </summary>
    private void SolveFootIk(MmdModel model, IkChain chain)
    {
        var ankle = chain.Bones[^1];
        var anklePos = ankle.Position;
        var origin = anklePos + new Vector3(0, 0.5f, 0);

        if (_physics.Raycast(origin, new Vector3(0, -1, 0), 1.0f, out var hit) && hit.HasHit)
        {
            chain.Target = hit.Position;
            if (model.RootBone != null)
            {
                float delta = hit.Position.Y - anklePos.Y;
                if (delta > 0)
                    model.RootBone.Position += new Vector3(0, delta, 0);
            }
        }
        else
        {
            chain.Target = anklePos;
        }

        SolveCcdChain(chain);
    }
}
