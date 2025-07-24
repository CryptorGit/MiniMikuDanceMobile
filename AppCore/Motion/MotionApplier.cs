using System.Numerics;
using System.Collections.Generic;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionApplier
{
    private readonly ModelData _model;
    private readonly Dictionary<BlazePoseJoint, int> _boneMap = new();
    private readonly float _modelHeight;

    public MotionApplier(ModelData model)
    {
        _model = model;
        MapBones();
        _modelHeight = EstimateModelHeight();
    }

    private static Vector3 ExtractTranslation(Matrix4x4 m)
    {
        return new Vector3(m.M41, m.M42, m.M43);
    }

    private float EstimateModelHeight()
    {
        if (_model.HumanoidBones.TryGetValue("hips", out int hipsIdx) &&
            _model.HumanoidBones.TryGetValue("head", out int headIdx) &&
            hipsIdx >= 0 && hipsIdx < _model.Bones.Count &&
            headIdx >= 0 && headIdx < _model.Bones.Count)
        {
            var hips = ExtractTranslation(_model.Bones[hipsIdx].BindMatrix);
            var head = ExtractTranslation(_model.Bones[headIdx].BindMatrix);
            return Vector3.Distance(hips, head);
        }
        return 1f;
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

    private static Quaternion BasisToQuat(Vector3 right, Vector3 up)
    {
        right = Vector3.Normalize(right);
        up = Vector3.Normalize(up);
        var forward = Vector3.Normalize(Vector3.Cross(right, up));
        var mat = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);
        return Quaternion.CreateFromRotationMatrix(mat);
    }

    private static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        forward = Vector3.Normalize(forward);
        var right = Vector3.Normalize(Vector3.Cross(up, forward));
        var realUp = Vector3.Cross(forward, right);
        var mat = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            realUp.X, realUp.Y, realUp.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);
        return Quaternion.CreateFromRotationMatrix(mat);
    }

    public (Dictionary<int, Quaternion> rotations, Matrix4x4 transform) Apply(JointData joint)
    {
        var rotations = new Dictionary<int, Quaternion>();
        if (joint.Positions.Length < 33)
            return (rotations, _model.Transform);
        static Vector3 MirrorFlip(Vector3 v) => new Vector3(-v.X, v.Y, -v.Z);

        // key landmarks (mirrored & flipped)
        var hipLRaw = MirrorFlip(joint.Positions[(int)BlazePoseJoint.LeftHip]);
        var hipRRaw = MirrorFlip(joint.Positions[(int)BlazePoseJoint.RightHip]);
        var eyeLRaw = MirrorFlip(joint.Positions[(int)BlazePoseJoint.LeftEye]);
        var eyeRRaw = MirrorFlip(joint.Positions[(int)BlazePoseJoint.RightEye]);
        var noseRaw = MirrorFlip(joint.Positions[(int)BlazePoseJoint.Nose]);

        // determine scale from hips-head distance
        var hipsPosRaw = (hipLRaw + hipRRaw) * 0.5f;
        var headPosRaw = (noseRaw + eyeLRaw + eyeRRaw) / 3f;
        var mpHeight = Vector3.Distance(hipsPosRaw, headPosRaw);
        float scale = mpHeight > 0 ? _modelHeight / mpHeight : 1f;
        Vector3 Scale(Vector3 v) => v * scale;

        var hipL = Scale(hipLRaw);
        var hipR = Scale(hipRRaw);
        var shoulderL = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.LeftShoulder]));
        var shoulderR = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.RightShoulder]));
        var elbowL = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.LeftElbow]));
        var elbowR = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.RightElbow]));
        var wristL = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.LeftWrist]));
        var wristR = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.RightWrist]));
        var kneeL = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.LeftKnee]));
        var kneeR = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.RightKnee]));
        var ankleL = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.LeftAnkle]));
        var ankleR = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.RightAnkle]));
        var eyeL = Scale(eyeLRaw);
        var eyeR = Scale(eyeRRaw);
        var nose = Scale(noseRaw);

        // intermediate points
        var hipsPos = (hipL + hipR) * 0.5f;
        var shoulderMid = (shoulderL + shoulderR) * 0.5f;
        var chestPos = shoulderMid;
        var spinePos = Vector3.Lerp(hipsPos, chestPos, 0.5f);
        var headPos = (nose + eyeL + eyeR) / 3f;
        var neckPos = chestPos + 0.35f * (headPos - chestPos);

        // hips
        var hipsRight = hipR - hipL;
        var hipsUp = chestPos - hipsPos;
        var hipsRot = BasisToQuat(hipsRight, hipsUp);
        if (_model.HumanoidBones.TryGetValue("hips", out var hipsIdx))
            rotations[hipsIdx] = hipsRot;

        // spine
        var spineUp = Vector3.Transform(Vector3.UnitY, hipsRot);
        var spineRot = LookRotation(chestPos - spinePos, spineUp);
        if (_model.HumanoidBones.TryGetValue("spine", out var spineIdx))
            rotations[spineIdx] = spineRot;

        // chest
        var chestRot = BasisToQuat(shoulderR - shoulderL, neckPos - chestPos);
        if (_model.HumanoidBones.TryGetValue("chest", out var chestIdx))
            rotations[chestIdx] = chestRot;

        // neck
        var neckRot = BasisToQuat(shoulderR - shoulderL, headPos - neckPos);
        if (_model.HumanoidBones.TryGetValue("neck", out var neckIdx))
            rotations[neckIdx] = neckRot;

        // head
        var headRot = BasisToQuat(eyeR - eyeL, headPos - neckPos);
        if (_model.HumanoidBones.TryGetValue("head", out var headIdx))
            rotations[headIdx] = headRot;

        var chestUp = Vector3.Transform(Vector3.UnitY, chestRot);

        // shoulders
        // Use chest center as origin so the rotation flows naturally from
        // neck -> chest -> shoulder -> elbow
        var lShoulderRot = LookRotation(shoulderL - chestPos, chestUp);
        if (_model.HumanoidBones.TryGetValue("leftShoulder", out var lshoIdx))
            rotations[lshoIdx] = lShoulderRot;

        var rShoulderRot = LookRotation(shoulderR - chestPos, chestUp);
        if (_model.HumanoidBones.TryGetValue("rightShoulder", out var rshoIdx))
            rotations[rshoIdx] = rShoulderRot;

        // arms
        var lUpperArmRot = LookRotation(elbowL - shoulderL, chestUp);
        if (_model.HumanoidBones.TryGetValue("leftUpperArm", out var luaIdx))
            rotations[luaIdx] = lUpperArmRot;

        var lLowerArmRot = LookRotation(wristL - elbowL, chestUp);
        if (_model.HumanoidBones.TryGetValue("leftLowerArm", out var llaIdx))
            rotations[llaIdx] = lLowerArmRot;

        var rUpperArmRot = LookRotation(elbowR - shoulderR, chestUp);
        if (_model.HumanoidBones.TryGetValue("rightUpperArm", out var ruaIdx))
            rotations[ruaIdx] = rUpperArmRot;

        var rLowerArmRot = LookRotation(wristR - elbowR, chestUp);
        if (_model.HumanoidBones.TryGetValue("rightLowerArm", out var rlaIdx))
            rotations[rlaIdx] = rLowerArmRot;

        // hands (approximation)
        var lHandDir = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.LeftIndex])) - wristL;
        var lThumbDir = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.LeftThumb])) - wristL;
        var lHandRot = BasisToQuat(lHandDir, Vector3.Cross(lHandDir, lThumbDir));
        if (_model.HumanoidBones.TryGetValue("leftHand", out var lhIdx))
            rotations[lhIdx] = lHandRot;

        var rHandDir = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.RightIndex])) - wristR;
        var rThumbDir = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.RightThumb])) - wristR;
        var rHandRot = BasisToQuat(rHandDir, Vector3.Cross(rHandDir, rThumbDir));
        if (_model.HumanoidBones.TryGetValue("rightHand", out var rhIdx))
            rotations[rhIdx] = rHandRot;

        var hipsUpVec = Vector3.Transform(Vector3.UnitY, hipsRot);

        // legs
        var lUpperLegRot = LookRotation(kneeL - hipL, hipsUpVec);
        if (_model.HumanoidBones.TryGetValue("leftUpperLeg", out var lulIdx))
            rotations[lulIdx] = lUpperLegRot;

        var lLowerLegRot = LookRotation(ankleL - kneeL, hipsUpVec);
        if (_model.HumanoidBones.TryGetValue("leftLowerLeg", out var lllIdx))
            rotations[lllIdx] = lLowerLegRot;

        var rUpperLegRot = LookRotation(kneeR - hipR, hipsUpVec);
        if (_model.HumanoidBones.TryGetValue("rightUpperLeg", out var rulIdx))
            rotations[rulIdx] = rUpperLegRot;

        var rLowerLegRot = LookRotation(ankleR - kneeR, hipsUpVec);
        if (_model.HumanoidBones.TryGetValue("rightLowerLeg", out var rllIdx))
            rotations[rllIdx] = rLowerLegRot;

        // feet
        var lFootDir = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.LeftFootIndex])) - ankleL;
        var lFootRot = LookRotation(lFootDir, hipsUpVec);
        if (_model.HumanoidBones.TryGetValue("leftFoot", out var lfIdx))
            rotations[lfIdx] = lFootRot;

        var rFootDir = Scale(MirrorFlip(joint.Positions[(int)BlazePoseJoint.RightFootIndex])) - ankleR;
        var rFootRot = LookRotation(rFootDir, hipsUpVec);
        if (_model.HumanoidBones.TryGetValue("rightFoot", out var rfIdx))
            rotations[rfIdx] = rFootRot;

        var transform = Matrix4x4.CreateTranslation(hipsPos);
        return (rotations, transform);
    }
}
