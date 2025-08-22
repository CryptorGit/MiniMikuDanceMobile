using Microsoft.Maui.Controls;
using MiniMikuDance.Physics;

namespace MiniMikuDanceMaui;

public partial class PhysicsView : ContentView
{
    public PhysicsView(PhysicsConfig config)
    {
        InitializeComponent();
        SetConfig(config);
    }

    public void SetConfig(PhysicsConfig config)
    {
        GravityLabel.Text = $"Gravity: {config.Gravity} m/s² (ModelScaleでスケーリング)";
        SolverLabel.Text = $"SolverIterationCount: {config.SolverIterationCount}";
        SubstepLabel.Text = $"SubstepCount: {config.SubstepCount}";
        DampingLabel.Text = $"Damping: {config.Damping:F2}";
        BoneBlendLabel.Text = $"BoneBlendFactor: {config.BoneBlendFactor:F2}";
        GroundHeightLabel.Text = $"GroundHeight: {config.GroundHeight:F2}";
        RestitutionLabel.Text = $"Restitution: {config.Restitution:F2}";
        FrictionLabel.Text = $"Friction: {config.Friction:F2}";
        LockTranslationLabel.Text = $"LockTranslation: {config.LockTranslation}";
    }
}
