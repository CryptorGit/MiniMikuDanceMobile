using System;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiniMikuDance.Physics;

public struct PhysicsConfig
{
    public Vector3 Gravity { get; set; }
    public int SolverIterationCount { get; set; }
    public int SubstepCount { get; set; }

    private readonly ILogger? _logger;

    private float damping;
    public float Damping
    {
        readonly get => damping;
        set => damping = Math.Clamp(value, 0f, 1f);
    }

    public float BoneBlendFactor { get; set; }
    public float GroundHeight { get; set; }
    private float restitution;
    /// <summary>反発係数 (0～1)。</summary>
    public float Restitution
    {
        readonly get => restitution;
        set => restitution = Math.Clamp(value, 0f, 1f);
    }

    private float restitutionRecoveryScale;
    /// <summary>Restitution から最大反発速度を計算する際のスケール係数 (m/s)。</summary>
    public float RestitutionRecoveryScale
    {
        readonly get => restitutionRecoveryScale;
        set
        {
            const float MaxLimit = 100f;
            var clamped = Math.Clamp(value, 0f, MaxLimit);
            if (clamped != value)
                _logger?.LogWarning("RestitutionRecoveryScale は 0～{Limit} の範囲に収めてください。入力値 {Value} を {Clamped} に補正します。", value, clamped, MaxLimit);
            restitutionRecoveryScale = clamped;
        }
    }

    private float friction;
    public float Friction
    {
        readonly get => friction;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (clamped != value)
                _logger?.LogWarning("Friction は 0～1 の範囲に収めてください。入力値 {Value} を {Clamped} に補正します。", value, clamped);
            friction = clamped;
        }
    }

    public bool LockTranslation { get; set; } = false;
    public int MaxThreadCount { get; set; }

    public PhysicsConfig(Vector3 gravity, int solverIterationCount, int substepCount, float damping = 0.98f, float boneBlendFactor = 0.5f, float groundHeight = 0f, float restitution = 0.2f, float restitutionRecoveryScale = 0.2f, float friction = 0.5f, bool lockTranslation = false, int maxThreadCount = 4, ILogger? logger = null)
    {
        Gravity = gravity;
        SolverIterationCount = solverIterationCount;
        SubstepCount = substepCount;
        _logger = logger ?? NullLogger.Instance;
        Damping = damping;
        BoneBlendFactor = boneBlendFactor;
        GroundHeight = groundHeight;
        Restitution = restitution;
        RestitutionRecoveryScale = restitutionRecoveryScale;
        Friction = friction;
        LockTranslation = lockTranslation;
        MaxThreadCount = maxThreadCount;
    }
}

