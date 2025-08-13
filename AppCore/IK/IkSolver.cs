using System;
using System.Collections.Generic;
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
            SolveCcdChain(model, chain);

        foreach (var chain in model.FootIkChains)
            SolveFootIk(model, chain);
    }

    /// <summary>
    /// CCD法で単一のIKチェーンを解決する。
    /// 到達不能な場合は胸部方向へチェーンを一時拡張して再試行する。
    /// </summary>
    private void SolveCcdChain(MmdModel model, IkChain chain)
    {
        const float epsilon = 1e-3f;
        void ExecuteCcd(List<int> links, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                var effectorIndex = links[0];
                var targetPos = model.Bones[chain.Target].Translation;
                var effectorPos = model.Bones[effectorIndex].Translation;
                if (Vector3.Distance(effectorPos, targetPos) <= epsilon)
                    break;

                for (int j = 0; j < links.Count; j++)
                {
                    var boneIndex = links[j];
                    var bone = model.Bones[boneIndex];
                    var bonePos = bone.Translation;
                    var toEffector = effectorPos - bonePos;
                    var toTarget = targetPos - bonePos;
                    if (toEffector.LengthSquared() < 1e-8f || toTarget.LengthSquared() < 1e-8f)
                        continue;

                    var axis = Vector3.Cross(toEffector, toTarget);
                    var axisLen = axis.Length();
                    if (axisLen < 1e-6f)
                        continue;
                    axis /= axisLen;
                    var angle = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(toEffector), Vector3.Normalize(toTarget)), -1f, 1f));
                    var rot = Quaternion.CreateFromAxisAngle(axis, angle);
                    bone.Rotation = Quaternion.Normalize(rot * bone.Rotation);
                    effectorPos = bonePos + Vector3.Transform(toEffector, rot);
                }

                if (Vector3.Distance(effectorPos, targetPos) <= epsilon)
                    break;
            }
        }

        var links = new List<int>(chain.Links);
        ExecuteCcd(links, chain.Iterations);

        var effectorIndexFinal = links[0];
        var effectorPosFinal = model.Bones[effectorIndexFinal].Translation;
        var targetPosFinal = model.Bones[chain.Target].Translation;
        if (Vector3.Distance(effectorPosFinal, targetPosFinal) > epsilon)
        {
            int parent = model.Bones[links[^1]].Parent;
            if (parent >= 0)
            {
                links.Add(parent);
                ExecuteCcd(links, Math.Min(10, chain.Iterations));
            }
        }
    }

    /// <summary>
    /// フットIKを解決する。
    /// 足首の位置から地面へレイキャストし、ターゲットを補正した上で CCD を適用する。
    /// 必要に応じてルートボーンを上方へ補正する。
    /// </summary>
    private void SolveFootIk(MmdModel model, FootIkChain chain)
    {
        var ankleBone = model.Bones[chain.Ankle];
        var anklePos = ankleBone.Translation;
        var origin = anklePos + new Vector3(0, 0.5f, 0);
        var direction = new Vector3(0, -1, 0);

        Vector3? hitPos = null;
        Quaternion? hitRot = null;

        if (_physics.Raycast(origin, direction, 1.0f, out var hit) && hit.HasHit)
        {
            hitPos = hit.Position;
            var normal = Vector3.Normalize(hit.Normal);
            var axis = Vector3.Cross(Vector3.UnitY, normal);
            var axisLen = axis.Length();
            if (axisLen > 1e-6f)
            {
                axis /= axisLen;
                var angle = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.UnitY, normal), -1f, 1f));
                hitRot = Quaternion.CreateFromAxisAngle(axis, angle);
            }

            var offsetY = hit.Position.Y - anklePos.Y;
            if (offsetY > 0)
            {
                var root = model.Bones[0];
                root.Translation += new Vector3(0, offsetY, 0);
                anklePos.Y += offsetY;
            }
        }

        if (hitPos.HasValue)
        {
            var targetBone = model.Bones[chain.Target];
            targetBone.Translation = hitPos.Value;
            if (hitRot.HasValue)
                targetBone.Rotation = hitRot.Value;
        }

        SolveCcdChain(model, chain);
    }
}
