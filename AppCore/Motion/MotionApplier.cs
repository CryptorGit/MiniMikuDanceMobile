using System.Numerics;
using System.Collections.Generic;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionApplier
{
    private readonly ModelData _model;
    private readonly Dictionary<BlazePoseJoint, int> _boneMap = new();
    private readonly IkSolver _solver;

    public MotionApplier(ModelData model)
    {
        _model = model;
        MapBones();
        _solver = new IkSolver(_model.Bones);
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
            }
        }

        void SolveIk(BlazePoseJoint rootJ, BlazePoseJoint midJ, BlazePoseJoint endJ)
        {
            if (!_boneMap.TryGetValue(rootJ, out int br) ||
                !_boneMap.TryGetValue(midJ, out int bm) ||
                !_boneMap.TryGetValue(endJ, out int be))
                return;
            if ((int)endJ >= joint.Positions.Length || (int)rootJ >= joint.Positions.Length)
                return;
            Vector3 target = joint.Positions[(int)endJ];
            var mid = _solver.Solve(br, bm, be, target);
            if (bm >= 0 && bm < _model.Bones.Count)
                _model.Bones[bm].Translation = mid;
        }

        SolveIk(BlazePoseJoint.LeftShoulder, BlazePoseJoint.LeftElbow, BlazePoseJoint.LeftWrist);
        SolveIk(BlazePoseJoint.RightShoulder, BlazePoseJoint.RightElbow, BlazePoseJoint.RightWrist);
        SolveIk(BlazePoseJoint.LeftHip, BlazePoseJoint.LeftKnee, BlazePoseJoint.LeftAnkle);
        SolveIk(BlazePoseJoint.RightHip, BlazePoseJoint.RightKnee, BlazePoseJoint.RightAnkle);

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
