using System.Numerics;
using System.Collections.Generic;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionApplier
{
    private readonly ModelData _model;
    private readonly Dictionary<BlazePoseJoint, int> _boneMap = new();

    public MotionApplier(ModelData model)
    {
        _model = model;
        MapBones();
    }

    private void MapBones()
    {
        void AddMap(BlazePoseJoint joint, string boneName)
        {
            if (_model.HumanoidBones.TryGetValue(boneName, out int idx))
            {
                _boneMap[joint] = idx;
            }
        }

        AddMap(BlazePoseJoint.LeftShoulder, "leftShoulder");
        AddMap(BlazePoseJoint.RightShoulder, "rightShoulder");
        AddMap(BlazePoseJoint.LeftElbow, "leftLowerArm");
        AddMap(BlazePoseJoint.RightElbow, "rightLowerArm");
        AddMap(BlazePoseJoint.LeftWrist, "leftHand");
        AddMap(BlazePoseJoint.RightWrist, "rightHand");
        AddMap(BlazePoseJoint.LeftHip, "leftUpperLeg");
        AddMap(BlazePoseJoint.RightHip, "rightUpperLeg");
        AddMap(BlazePoseJoint.LeftKnee, "leftLowerLeg");
        AddMap(BlazePoseJoint.RightKnee, "rightLowerLeg");
        AddMap(BlazePoseJoint.LeftAnkle, "leftFoot");
        AddMap(BlazePoseJoint.RightAnkle, "rightFoot");
        AddMap(BlazePoseJoint.Nose, "head");
    }

    public void Apply(JointData joint)
    {
        foreach (var kv in _boneMap)
        {
            int jIndex = (int)kv.Key;
            if (jIndex >= joint.Positions.Length)
                continue;
            int bIndex = kv.Value;
            if (bIndex >= 0 && bIndex < _model.Bones.Count)
            {
                _model.Bones[bIndex].Translation = joint.Positions[jIndex];
                if (jIndex < joint.Rotations.Length)
                {
                    var e = joint.Rotations[jIndex];
                    const float deg2rad = MathF.PI / 180f;
                    var q = Quaternion.CreateFromYawPitchRoll(
                        e.Y * deg2rad,
                        e.X * deg2rad,
                        e.Z * deg2rad);
                    _model.Bones[bIndex].Rotation = q;
                }
            }
        }

        if (joint.Positions.Length > (int)BlazePoseJoint.RightHip)
        {
            var lh = joint.Positions[(int)BlazePoseJoint.LeftHip];
            var rh = joint.Positions[(int)BlazePoseJoint.RightHip];
            var hip = (lh + rh) * 0.5f;
            var trans = Matrix4x4.CreateTranslation(hip);
            _model.Transform = trans;
        }
    }
}
