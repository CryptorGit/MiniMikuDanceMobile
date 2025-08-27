using System;
using System.Numerics;
using MiniMikuDance.App;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiniMikuDance.Physics;

public sealed class NullPhysicsWorld : IPhysicsWorld
{
    private readonly ILogger _logger;

    public NullPhysicsWorld(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public void Initialize(float modelScale) { }
    public void Step(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0f)
        {
            _logger.LogWarning("dt が不正な値のため Step をスキップします: {Dt}", dt);
            return;
        }
    }
    public void SyncFromBones(Scene scene) { }
    public void SyncToBones(Scene scene) { }
    public Vector3 GetGravity() => Vector3.Zero;
    public void Dispose() { }
}
