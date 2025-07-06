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
        if (joint.Positions.Length == 0 || _model.Bones.Count == 0)
            return;

        for (int i = 0; i < _model.Bones.Count && i < joint.Positions.Length; i++)
        {
            var bone = _model.Bones[i];
            int parent = bone.Parent;
            Vector3 world = joint.Positions[i];
            Vector3 parentWorld = (parent >= 0 && parent < joint.Positions.Length)
                ? joint.Positions[parent]
                : Vector3.Zero;

            bone.Translation = world - parentWorld;

            if (parent >= 0 && parent < joint.Positions.Length)
            {
                Vector3 dir = world - parentWorld;
                if (dir.LengthSquared() > 1e-6f)
                {
                    dir = Vector3.Normalize(dir);
                    var axis = Vector3.Cross(Vector3.UnitY, dir);
                    float len = axis.Length();
                    if (len > 1e-6f)
                    {
                        axis /= len;
                        float dot = Math.Clamp(Vector3.Dot(Vector3.UnitY, dir), -1f, 1f);
                        float angle = MathF.Acos(dot);
                        bone.Rotation = Quaternion.CreateFromAxisAngle(axis, angle);
                    }
                    else
                    {
                        float dot = Math.Clamp(Vector3.Dot(Vector3.UnitY, dir), -1f, 1f);
                        bone.Rotation = dot >= 0f
                            ? Quaternion.Identity
                            : Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
                    }
                }
                else
                {
                    bone.Rotation = Quaternion.Identity;
                }
            }
            else
            {
                bone.Rotation = Quaternion.Identity;
            }
        }
    }
}
