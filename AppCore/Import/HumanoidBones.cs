using System.Collections.Generic;

namespace MiniMikuDance.Import;

/// <summary>
/// PMXモデルで一般的に利用される主要17ボーンの順序と、
/// 日本語/英語名から内部名称への対応表を提供する。
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

    /// <summary>
    /// PMXでよく使われるボーン名から、内部で利用するヒューマノイドボーン名へのマッピング。
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> BoneNameMap = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        // 体幹
        {"センター", "hips"}, {"下半身", "hips"}, {"center", "hips"}, {"hips", "hips"},
        {"上半身", "spine"}, {"spine", "spine"},
        {"上半身2", "chest"}, {"胸", "chest"}, {"chest", "chest"},
        {"首", "neck"}, {"neck", "neck"},
        {"頭", "head"}, {"head", "head"},

        // 左腕
        {"左腕", "leftUpperArm"}, {"左肩", "leftUpperArm"}, {"leftArm", "leftUpperArm"},
        {"左ひじ", "leftLowerArm"}, {"leftElbow", "leftLowerArm"},
        {"左手首", "leftHand"}, {"leftWrist", "leftHand"},

        // 右腕
        {"右腕", "rightUpperArm"}, {"右肩", "rightUpperArm"}, {"rightArm", "rightUpperArm"},
        {"右ひじ", "rightLowerArm"}, {"rightElbow", "rightLowerArm"},
        {"右手首", "rightHand"}, {"rightWrist", "rightHand"},

        // 左脚
        {"左足", "leftUpperLeg"}, {"leftLeg", "leftUpperLeg"},
        {"左ひざ", "leftLowerLeg"}, {"leftKnee", "leftLowerLeg"},
        {"左足首", "leftFoot"}, {"leftAnkle", "leftFoot"},

        // 右脚
        {"右足", "rightUpperLeg"}, {"rightLeg", "rightUpperLeg"},
        {"右ひざ", "rightLowerLeg"}, {"rightKnee", "rightLowerLeg"},
        {"右足首", "rightFoot"}, {"rightAnkle", "rightFoot"},
    };
}
