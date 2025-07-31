using System;
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
        => new(m.M41, m.M42, m.M43);

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

    private static Vector3 Normalize(Vector3 v)
    {
        float len = v.Length();
        return len > 0 ? v / len : Vector3.Zero;
    }

    private static Matrix4x4 ComputeMatrix(Vector3 v1, Vector3 v2)
    {
        var z = Normalize(v1);
        var xTemp = Normalize(v2);
        if (Vector3.Cross(z, xTemp).LengthSquared() < 1e-6f)
            xTemp = MathF.Abs(z.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitZ;
        var y = Vector3.Normalize(Vector3.Cross(z, xTemp));
        var x = Vector3.Normalize(Vector3.Cross(y, z));
        return new Matrix4x4(
            x.X, x.Y, x.Z, 0,
            y.X, y.Y, y.Z, 0,
            z.X, z.Y, z.Z, 0,
            0, 0, 0, 1);
    }

    private static Quaternion MatrixToQuat(Matrix4x4 m)
        => Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(m));

    private static Quaternion QInv(Quaternion q)
        => new(-q.X, -q.Y, -q.Z, q.W);

    private static Quaternion QMul(Quaternion a, Quaternion b)
        => Quaternion.Normalize(Quaternion.Concatenate(a, b));

    public (Dictionary<int, Quaternion> rotations, Matrix4x4 transform) Apply(JointData joint)
    {
        var worldRot = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);
        var localRot = new Dictionary<int, Quaternion>();
        if (joint.Positions.Length < 33)
            return (localRot, _model.Transform);

        Vector3[] pts = new Vector3[joint.Positions.Length];
        for (int i = 0; i < pts.Length; i++)
        {
            var p = joint.Positions[i];
            pts[i] = new Vector3(-p.X, p.Y, -p.Z);
        }

        var hipLRaw = pts[(int)BlazePoseJoint.LeftHip];
        var hipRRaw = pts[(int)BlazePoseJoint.RightHip];
        var noseRaw = pts[(int)BlazePoseJoint.Nose];
        var eyeLRaw = pts[(int)BlazePoseJoint.LeftEye];
        var eyeRRaw = pts[(int)BlazePoseJoint.RightEye];

        var hipCenterRaw = (hipLRaw + hipRRaw) * 0.5f;
        var headRaw = (noseRaw + eyeLRaw + eyeRRaw) / 3f;
        float mpHeight = Vector3.Distance(hipCenterRaw, headRaw);
        float scale = mpHeight > 0 ? _modelHeight / mpHeight : 1f;

        for (int i = 0; i < pts.Length; i++)
            pts[i] *= scale;

        Vector3 nose = pts[(int)BlazePoseJoint.Nose];
        Vector3 hipL = pts[(int)BlazePoseJoint.LeftHip];
        Vector3 hipR = pts[(int)BlazePoseJoint.RightHip];
        Vector3 shoulderL = pts[(int)BlazePoseJoint.LeftShoulder];
        Vector3 shoulderR = pts[(int)BlazePoseJoint.RightShoulder];
        Vector3 elbowL = pts[(int)BlazePoseJoint.LeftElbow];
        Vector3 elbowR = pts[(int)BlazePoseJoint.RightElbow];
        Vector3 wristL = pts[(int)BlazePoseJoint.LeftWrist];
        Vector3 wristR = pts[(int)BlazePoseJoint.RightWrist];
        Vector3 kneeL = pts[(int)BlazePoseJoint.LeftKnee];
        Vector3 kneeR = pts[(int)BlazePoseJoint.RightKnee];
        Vector3 ankleL = pts[(int)BlazePoseJoint.LeftAnkle];
        Vector3 ankleR = pts[(int)BlazePoseJoint.RightAnkle];
        Vector3 heelL = pts[(int)BlazePoseJoint.LeftHeel];
        Vector3 heelR = pts[(int)BlazePoseJoint.RightHeel];
        Vector3 footIndexL = pts[(int)BlazePoseJoint.LeftFootIndex];
        Vector3 footIndexR = pts[(int)BlazePoseJoint.RightFootIndex];
        Vector3 indexL = pts[(int)BlazePoseJoint.LeftIndex];
        Vector3 indexR = pts[(int)BlazePoseJoint.RightIndex];
        Vector3 thumbL = pts[(int)BlazePoseJoint.LeftThumb];
        Vector3 thumbR = pts[(int)BlazePoseJoint.RightThumb];
        Vector3 leftEar = pts[(int)BlazePoseJoint.LeftEar];
        Vector3 rightEar = pts[(int)BlazePoseJoint.RightEar];

        Vector3 hipCenter = (hipL + hipR) * 0.5f;
        Vector3 chest = (shoulderL + shoulderR) * 0.5f;
        Vector3 spine = Vector3.Lerp(hipCenter, chest, 0.5f);
        Vector3 head = (nose + leftEar + rightEar) / 3f;
        Vector3 neck = chest + 0.35f * (head - chest);

        worldRot["hips"] = MatrixToQuat(ComputeMatrix(chest - hipCenter, hipR - hipL));
        worldRot["spine"] = MatrixToQuat(ComputeMatrix(chest - spine, hipR - hipL));
        worldRot["chest"] = MatrixToQuat(ComputeMatrix(neck - chest, shoulderR - shoulderL));
        worldRot["neck"] = MatrixToQuat(ComputeMatrix(head - neck, shoulderR - shoulderL));
        worldRot["head"] = MatrixToQuat(ComputeMatrix(nose - neck, rightEar - leftEar));

        worldRot["leftUpperArm"] = MatrixToQuat(ComputeMatrix(elbowL - shoulderL, wristL - elbowL));
        worldRot["leftLowerArm"] = MatrixToQuat(ComputeMatrix(wristL - elbowL, indexL - wristL));
        worldRot["leftHand"] = MatrixToQuat(ComputeMatrix(indexL - wristL, Vector3.Cross(indexL - wristL, thumbL - wristL)));

        worldRot["rightUpperArm"] = MatrixToQuat(ComputeMatrix(elbowR - shoulderR, wristR - elbowR));
        worldRot["rightLowerArm"] = MatrixToQuat(ComputeMatrix(wristR - elbowR, indexR - wristR));
        worldRot["rightHand"] = MatrixToQuat(ComputeMatrix(indexR - wristR, Vector3.Cross(indexR - wristR, thumbR - wristR)));

        worldRot["leftUpperLeg"] = MatrixToQuat(ComputeMatrix(kneeL - hipL, ankleL - kneeL));
        worldRot["leftLowerLeg"] = MatrixToQuat(ComputeMatrix(ankleL - kneeL, footIndexL - ankleL));
        worldRot["leftFoot"] = MatrixToQuat(ComputeMatrix(footIndexL - ankleL, footIndexL - heelL));

        worldRot["rightUpperLeg"] = MatrixToQuat(ComputeMatrix(kneeR - hipR, ankleR - kneeR));
        worldRot["rightLowerLeg"] = MatrixToQuat(ComputeMatrix(ankleR - kneeR, footIndexR - ankleR));
        worldRot["rightFoot"] = MatrixToQuat(ComputeMatrix(footIndexR - ankleR, footIndexR - heelR));

        Quaternion GetWorld(string n) => worldRot.TryGetValue(n, out var q) ? q : Quaternion.Identity;

        void SetLocal(string name, string? parent)
        {
            if (!_model.HumanoidBones.TryGetValue(name, out int idx))
                return;
            var q = GetWorld(name);
            if (parent != null)
                q = QMul(QInv(GetWorld(parent)), q);
            localRot[idx] = q;
        }

        SetLocal("hips", null);
        SetLocal("spine", "hips");
        SetLocal("chest", "spine");
        SetLocal("neck", "chest");
        SetLocal("head", "neck");
        SetLocal("leftUpperArm", "chest");
        SetLocal("leftLowerArm", "leftUpperArm");
        SetLocal("leftHand", "leftLowerArm");
        SetLocal("rightUpperArm", "chest");
        SetLocal("rightLowerArm", "rightUpperArm");
        SetLocal("rightHand", "rightLowerArm");
        SetLocal("leftUpperLeg", "hips");
        SetLocal("leftLowerLeg", "leftUpperLeg");
        SetLocal("leftFoot", "leftLowerLeg");
        SetLocal("rightUpperLeg", "hips");
        SetLocal("rightLowerLeg", "rightUpperLeg");
        SetLocal("rightFoot", "rightLowerLeg");

        var transform = Matrix4x4.CreateTranslation(hipCenter);
        return (localRot, transform);
    }
}
