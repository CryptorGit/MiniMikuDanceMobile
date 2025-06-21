using System.Numerics;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionApplier
{
    private readonly ModelData _model;

    public MotionApplier(ModelData model)
    {
        _model = model;
    }

    public void Apply(JointData joint)
    {
        // For demonstration, rotate the model around Y axis using first joint X value
        float angle = joint.Positions.Length > 0 ? joint.Positions[0].X : 0f;
        _model.Transform = Matrix4x4.CreateRotationY(angle);
    }
}
