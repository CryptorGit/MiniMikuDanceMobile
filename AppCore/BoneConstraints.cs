using System.Numerics;
using System.Collections.Generic;

namespace MiniMikuDance;

/// <summary>
/// ヒューマノイドボーンごとの回転制約を保持するクラス。
/// 数値は仮決めであり、後から調整可能。
/// </summary>
public class BoneConstraints
{
    public record Limit(Vector3 Min, Vector3 Max);

    private readonly Dictionary<string, Limit> _map = new();

    public Limit? GetLimit(string boneName)
    {
        return _map.TryGetValue(boneName, out var l) ? l : null;
    }

    public Vector3 Clip(string boneName, Vector3 euler)
    {
        if (_map.TryGetValue(boneName, out var l))
        {
            return Vector3.Clamp(euler, l.Min, l.Max);
        }
        return euler;
    }

    /// <summary>
    /// デフォルト制約を生成する。値は暫定であり要調整。
    /// </summary>
    public static BoneConstraints CreateDefault()
    {
        var bc = new BoneConstraints();
        // TODO: 実測に基づき閾値を調整する
        bc._map["hips"] = new Limit(new Vector3(-30, -30, -30), new Vector3(30, 30, 30));
        bc._map["spine"] = new Limit(new Vector3(-20, -30, -20), new Vector3(20, 30, 20));
        bc._map["chest"] = new Limit(new Vector3(-30, -40, -25), new Vector3(30, 40, 25));
        bc._map["neck"] = new Limit(new Vector3(-40, -40, -40), new Vector3(40, 40, 40));
        bc._map["head"] = new Limit(new Vector3(-45, -60, -45), new Vector3(45, 60, 45));
        bc._map["leftUpperArm"] = new Limit(new Vector3(-90, -45, -90), new Vector3(90, 135, 90));
        bc._map["leftLowerArm"] = new Limit(new Vector3(-10, 0, -150), new Vector3(10, 0, 0));
        bc._map["leftHand"] = new Limit(new Vector3(-30, -60, -80), new Vector3(30, 60, 80));
        bc._map["rightUpperArm"] = new Limit(new Vector3(-90, -135, -90), new Vector3(90, 45, 90));
        bc._map["rightLowerArm"] = new Limit(new Vector3(-10, 0, 0), new Vector3(10, 0, 150));
        bc._map["rightHand"] = new Limit(new Vector3(-30, -60, -80), new Vector3(30, 60, 80));
        bc._map["leftUpperLeg"] = new Limit(new Vector3(-100, -30, -40), new Vector3(100, 120, 40));
        bc._map["leftLowerLeg"] = new Limit(new Vector3(0, 0, -5), new Vector3(160, 0, 5));
        bc._map["leftFoot"] = new Limit(new Vector3(-30, -20, -20), new Vector3(30, 45, 20));
        bc._map["rightUpperLeg"] = new Limit(new Vector3(-100, -120, -40), new Vector3(100, 30, 40));
        bc._map["rightLowerLeg"] = new Limit(new Vector3(0, 0, -5), new Vector3(160, 0, 5));
        bc._map["rightFoot"] = new Limit(new Vector3(-30, -20, -20), new Vector3(30, 45, 20));
        return bc;
    }
}
