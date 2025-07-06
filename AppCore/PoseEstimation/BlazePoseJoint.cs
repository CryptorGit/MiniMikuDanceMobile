namespace MiniMikuDance.PoseEstimation;

/// <summary>
/// BlazePoseの33関節を表す列挙体。
/// インデックス番号はONNXモデルの出力順に合わせています。
/// </summary>
public enum BlazePoseJoint
{
    Nose = 0,
    LeftEyeInner = 1,
    LeftEye = 2,
    LeftEyeOuter = 3,
    RightEyeInner = 4,
    RightEye = 5,
    RightEyeOuter = 6,
    LeftEar = 7,
    RightEar = 8,
    MouthLeft = 9,
    MouthRight = 10,
    LeftShoulder = 11,
    RightShoulder = 12,
    LeftElbow = 13,
    RightElbow = 14,
    LeftWrist = 15,
    RightWrist = 16,
    LeftPinky = 17,
    RightPinky = 18,
    LeftIndex = 19,
    RightIndex = 20,
    LeftThumb = 21,
    RightThumb = 22,
    LeftHip = 23,
    RightHip = 24,
    LeftKnee = 25,
    RightKnee = 26,
    LeftAnkle = 27,
    RightAnkle = 28,
    LeftHeel = 29,
    RightHeel = 30,
    LeftFootIndex = 31,
    RightFootIndex = 32
}

