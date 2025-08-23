using Microsoft.Maui.Controls;
using MiniMikuDance.Physics;
using MiniMikuDance.Import;
using System.Collections.Generic;
using System.Numerics;

namespace MiniMikuDanceMaui;

public partial class PhysicsView : ContentView
{
    private sealed class RigidBodyInfo
    {
        public string Name { get; init; } = string.Empty;
        public string Bone { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Shape { get; init; } = string.Empty;
        public string Size { get; init; } = string.Empty;
        public string Offset { get; init; } = string.Empty;
        public string Mass { get; init; } = string.Empty;
        public string Damping { get; init; } = string.Empty;
        public string Restitution { get; init; } = string.Empty;
        public string Friction { get; init; } = string.Empty;
    }

    public PhysicsView(PhysicsConfig config, bool useScaledGravity, IEnumerable<RigidBodyData> rigidBodies, IList<BoneData> bones)
    {
        InitializeComponent();
        SetConfig(config, useScaledGravity);
        SetRigidBodies(rigidBodies, bones);
    }

    public void SetConfig(PhysicsConfig config, bool useScaledGravity)
    {
        const float threshold = 1e-3f;
        const float max = 1e3f;

        var gravity = config.Gravity;
        var defaultGravity = new Vector3(0f, -9.81f, 0f);
        bool gravityWarn = false;
        string gravityWarnMessage = string.Empty;
        bool gravityInvalid =
            float.IsNaN(gravity.X) || float.IsNaN(gravity.Y) || float.IsNaN(gravity.Z) ||
            float.IsInfinity(gravity.X) || float.IsInfinity(gravity.Y) || float.IsInfinity(gravity.Z) ||
            System.MathF.Abs(gravity.X) > max || System.MathF.Abs(gravity.Y) > max || System.MathF.Abs(gravity.Z) > max;
        if (gravityInvalid)
        {
            gravity = defaultGravity;
            gravityWarn = true;
            gravityWarnMessage = "極端または無効な値をデフォルトに差し替え";
        }
        else
        {
            if (System.MathF.Abs(gravity.X) < threshold) { gravity.X = defaultGravity.X; gravityWarn = true; }
            if (System.MathF.Abs(gravity.Y) < threshold) { gravity.Y = defaultGravity.Y; gravityWarn = true; }
            if (System.MathF.Abs(gravity.Z) < threshold) { gravity.Z = defaultGravity.Z; gravityWarn = true; }
            if (gravityWarn) gravityWarnMessage = "極端に小さい値をデフォルトに差し替え";
        }
        GravityLabel.Text = $"Gravity: ({gravity.X:F2}, {gravity.Y:F2}, {gravity.Z:F2}) m/s²" + (useScaledGravity ? " (ModelScaleでスケーリング)" : string.Empty);
        if (gravityWarn) GravityLabel.Text += $" [警告: {gravityWarnMessage}]";

        float damping = config.Damping;
        if (System.MathF.Abs(damping) < threshold)
        {
            damping = 0.98f;
            DampingLabel.Text = $"Damping: {damping:F2} [警告: 極端に小さい値をデフォルトに差し替え]";
        }
        else
        {
            DampingLabel.Text = $"Damping: {damping:F2}";
        }

        float boneBlend = config.BoneBlendFactor;
        if (System.MathF.Abs(boneBlend) < threshold)
        {
            boneBlend = 0.5f;
            BoneBlendLabel.Text = $"BoneBlendFactor: {boneBlend:F2} [警告: 極端に小さい値をデフォルトに差し替え]";
        }
        else
        {
            BoneBlendLabel.Text = $"BoneBlendFactor: {boneBlend:F2}";
        }

        float groundHeight = config.GroundHeight;
        if (System.MathF.Abs(groundHeight) < threshold)
        {
            groundHeight = 0f;
            GroundHeightLabel.Text = $"GroundHeight: {groundHeight:F2} [警告: 極端に小さい値をデフォルトに差し替え]";
        }
        else
        {
            GroundHeightLabel.Text = $"GroundHeight: {groundHeight:F2}";
        }

        float restitution = config.Restitution;
        if (System.MathF.Abs(restitution) < threshold)
        {
            restitution = 0.2f;
            RestitutionLabel.Text = $"Restitution: {restitution:F2} [警告: 極端に小さい値をデフォルトに差し替え]";
        }
        else
        {
            RestitutionLabel.Text = $"Restitution: {restitution:F2}";
        }

        float friction = config.Friction;
        if (System.MathF.Abs(friction) < threshold)
        {
            friction = 0.5f;
            FrictionLabel.Text = $"Friction: {friction:F2} [警告: 極端に小さい値をデフォルトに差し替え]";
        }
        else
        {
            FrictionLabel.Text = $"Friction: {friction:F2}";
        }

        SolverLabel.Text = $"SolverIterationCount: {config.SolverIterationCount}";
        SubstepLabel.Text = $"SubstepCount: {config.SubstepCount}";
        LockTranslationLabel.Text = $"LockTranslation: {config.LockTranslation}";
    }

    public void SetRigidBodies(IEnumerable<RigidBodyData> rigidBodies, IList<BoneData> bones)
    {
        var list = new List<RigidBodyInfo>();
        foreach (var rb in rigidBodies)
        {
            var boneName = rb.BoneIndex >= 0 && rb.BoneIndex < bones.Count ? bones[rb.BoneIndex].Name : "";
            var size = $"{rb.Size.X:F2},{rb.Size.Y:F2},{rb.Size.Z:F2}";
            var offset = $"{rb.Position.X:F2},{rb.Position.Y:F2},{rb.Position.Z:F2}";
            var damping = $"{rb.LinearDamping:F2}/{rb.AngularDamping:F2}";
            list.Add(new RigidBodyInfo
            {
                Name = rb.Name,
                Bone = boneName,
                Type = rb.Mode.ToString(),
                Shape = rb.Shape.ToString(),
                Size = size,
                Offset = offset,
                Mass = rb.Mass.ToString("F2"),
                Damping = damping,
                Restitution = rb.Restitution.ToString("F2"),
                Friction = rb.Friction.ToString("F2"),
            });
        }
        RigidBodyList.ItemsSource = list;
    }
}
