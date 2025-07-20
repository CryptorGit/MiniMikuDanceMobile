namespace MiniMikuDance.Import;

/// <summary>
/// VRM仕様で定義される主要17ボーンの順序を提供する。
/// </summary>
public static class HumanoidBones
{
    public static readonly string[] StandardOrder = new[]
    {
        "hips", "spine", "chest", "neck", "head",
        "leftUpperArm", "leftLowerArm", "leftHand",
        "rightUpperArm", "rightLowerArm", "rightHand",
        "leftUpperLeg", "leftLowerLeg", "leftFoot",
        "rightUpperLeg", "rightLowerLeg", "rightFoot"
    };
}
