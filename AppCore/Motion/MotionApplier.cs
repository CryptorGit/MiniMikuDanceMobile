using System.Numerics;
using System.Collections.Generic;
using MiniMikuDance.Import;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.Motion;

public class MotionApplier
{
    private readonly ModelData _model;
    private readonly float _modelHeight;

    public MotionApplier(ModelData model)
    {
        _model = model;
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

        // Convert all landmarks to model space first
        var points = new Vector3[joint.Positions.Length];
        for (int i = 0; i < points.Length; i++)
            points[i] = MirrorFlip(joint.Positions[i]);

        var hipLRaw = points[(int)BlazePoseJoint.LeftHip];
        var hipRRaw = points[(int)BlazePoseJoint.RightHip];
        var eyeLRaw = points[(int)BlazePoseJoint.LeftEye];
        var eyeRRaw = points[(int)BlazePoseJoint.RightEye];
        var noseRaw = points[(int)BlazePoseJoint.Nose];

        // determine scale from hips-head distance
        var hipsPosRaw = (hipLRaw + hipRRaw) * 0.5f;
        var headPosRaw = (noseRaw + eyeLRaw + eyeRRaw) / 3f;
        var mpHeight = Vector3.Distance(hipsPosRaw, headPosRaw);
        float scale = mpHeight > 0 ? _modelHeight / mpHeight : 1f;

        for (int i = 0; i < points.Length; i++)
            points[i] *= scale;

        var hipL = points[(int)BlazePoseJoint.LeftHip];
        var hipR = points[(int)BlazePoseJoint.RightHip];
        var shoulderL = points[(int)BlazePoseJoint.LeftShoulder];
        var shoulderR = points[(int)BlazePoseJoint.RightShoulder];
        var elbowL = points[(int)BlazePoseJoint.LeftElbow];
        var elbowR = points[(int)BlazePoseJoint.RightElbow];
        var wristL = points[(int)BlazePoseJoint.LeftWrist];
        var wristR = points[(int)BlazePoseJoint.RightWrist];
        var kneeL = points[(int)BlazePoseJoint.LeftKnee];
        var kneeR = points[(int)BlazePoseJoint.RightKnee];
        var ankleL = points[(int)BlazePoseJoint.LeftAnkle];
        var ankleR = points[(int)BlazePoseJoint.RightAnkle];
        var eyeL = points[(int)BlazePoseJoint.LeftEye];
        var eyeR = points[(int)BlazePoseJoint.RightEye];
        var nose = points[(int)BlazePoseJoint.Nose];

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
        var lUpperArmY = elbowL - shoulderL;
        var lForearmVec = wristL - elbowL;
        var lUpperArmZ = Vector3.Cross(lUpperArmY, lForearmVec);
        var lUpperArmRot = BasisToQuat(lUpperArmZ, lUpperArmY);
        if (_model.HumanoidBones.TryGetValue("leftUpperArm", out var luaIdx))
            rotations[luaIdx] = lUpperArmRot;

        var lLowerArmY = wristL - elbowL;
        var lPalmNorm = Vector3.Cross(points[(int)BlazePoseJoint.LeftPinky] - wristL,
                                      points[(int)BlazePoseJoint.LeftIndex] - wristL);
        var lLowerArmRot = BasisToQuat(lPalmNorm, lLowerArmY);
        if (_model.HumanoidBones.TryGetValue("leftLowerArm", out var llaIdx))
            rotations[llaIdx] = lLowerArmRot;

        var rUpperArmY = elbowR - shoulderR;
        var rForearmVec = wristR - elbowR;
        var rUpperArmZ = Vector3.Cross(rUpperArmY, rForearmVec);
        var rUpperArmRot = BasisToQuat(rUpperArmZ, rUpperArmY);
        if (_model.HumanoidBones.TryGetValue("rightUpperArm", out var ruaIdx))
            rotations[ruaIdx] = rUpperArmRot;

        var rLowerArmY = wristR - elbowR;
        var rPalmNorm = Vector3.Cross(points[(int)BlazePoseJoint.RightPinky] - wristR,
                                      points[(int)BlazePoseJoint.RightIndex] - wristR);
        var rLowerArmRot = BasisToQuat(rPalmNorm, rLowerArmY);
        if (_model.HumanoidBones.TryGetValue("rightLowerArm", out var rlaIdx))
            rotations[rlaIdx] = rLowerArmRot;

        // hands (approximation)
        var lHandDir = points[(int)BlazePoseJoint.LeftIndex] - wristL;
        var lThumbDir = points[(int)BlazePoseJoint.LeftThumb] - wristL;
        var lHandRot = BasisToQuat(lHandDir, Vector3.Cross(lHandDir, lThumbDir));
        if (_model.HumanoidBones.TryGetValue("leftHand", out var lhIdx))
            rotations[lhIdx] = lHandRot;

        var rHandDir = points[(int)BlazePoseJoint.RightIndex] - wristR;
        var rThumbDir = points[(int)BlazePoseJoint.RightThumb] - wristR;
        var rHandRot = BasisToQuat(rHandDir, Vector3.Cross(rHandDir, rThumbDir));
        if (_model.HumanoidBones.TryGetValue("rightHand", out var rhIdx))
            rotations[rhIdx] = rHandRot;

        var hipsUpVec = Vector3.Transform(Vector3.UnitY, hipsRot);

        // legs
        var lUpperLegY = kneeL - hipL;
        var lLowerVec = ankleL - kneeL;
        var lUpperLegZ = Vector3.Cross(lUpperLegY, lLowerVec);
        var lUpperLegRot = BasisToQuat(lUpperLegZ, lUpperLegY);
        if (_model.HumanoidBones.TryGetValue("leftUpperLeg", out var lulIdx))
            rotations[lulIdx] = lUpperLegRot;

        var lLowerLegY = ankleL - kneeL;
        var lFootNorm = Vector3.Cross(points[(int)BlazePoseJoint.LeftHeel] - ankleL,
                                      points[(int)BlazePoseJoint.LeftFootIndex] - ankleL);
        var lLowerLegRot = BasisToQuat(lFootNorm, lLowerLegY);
        if (_model.HumanoidBones.TryGetValue("leftLowerLeg", out var lllIdx))
            rotations[lllIdx] = lLowerLegRot;

        var rUpperLegY = kneeR - hipR;
        var rLowerVec = ankleR - kneeR;
        var rUpperLegZ = Vector3.Cross(rUpperLegY, rLowerVec);
        var rUpperLegRot = BasisToQuat(rUpperLegZ, rUpperLegY);
        if (_model.HumanoidBones.TryGetValue("rightUpperLeg", out var rulIdx))
            rotations[rulIdx] = rUpperLegRot;

        var rLowerLegY = ankleR - kneeR;
        var rFootNorm = Vector3.Cross(points[(int)BlazePoseJoint.RightHeel] - ankleR,
                                      points[(int)BlazePoseJoint.RightFootIndex] - ankleR);
        var rLowerLegRot = BasisToQuat(rFootNorm, rLowerLegY);
        if (_model.HumanoidBones.TryGetValue("rightLowerLeg", out var rllIdx))
            rotations[rllIdx] = rLowerLegRot;

        // feet
        var lFootY = points[(int)BlazePoseJoint.LeftFootIndex] - ankleL;
        var lFootZ = Vector3.Cross(lFootY, points[(int)BlazePoseJoint.LeftHeel] - ankleL);
        var lFootRot = BasisToQuat(lFootZ, lFootY);
        if (_model.HumanoidBones.TryGetValue("leftFoot", out var lfIdx))
            rotations[lfIdx] = lFootRot;

        var rFootY = points[(int)BlazePoseJoint.RightFootIndex] - ankleR;
        var rFootZ = Vector3.Cross(rFootY, points[(int)BlazePoseJoint.RightHeel] - ankleR);
        var rFootRot = BasisToQuat(rFootZ, rFootY);
        if (_model.HumanoidBones.TryGetValue("rightFoot", out var rfIdx))
            rotations[rfIdx] = rFootRot;

        var transform = Matrix4x4.CreateTranslation(hipsPos);
        return (rotations, transform);
    }
}
