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
        if (joint.Positions.Length == 0)
            return;

        var pos = joint.Positions[0];
        var rotAngle = joint.Positions.Length > 1 ? joint.Positions[1].Y : 0f;
        var trans = Matrix4x4.CreateTranslation(pos);
        var rot = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, rotAngle);
        _model.Transform = rot * trans;
    }
}
