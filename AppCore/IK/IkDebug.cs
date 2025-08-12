using System.Diagnostics;
using System.Numerics;

namespace MiniMikuDance.IK;

/// <summary>
/// IK 計算のデバッグ用ユーティリティ。
/// <para>座標系の確認のために軸ベクトルをログ出力する。</para>
/// </summary>
public static class IkDebug
{
    /// <summary>
    /// 軸ベクトルのログ出力を有効にするかどうか。
    /// </summary>
    public static bool EnableAxisLogging { get; set; }

    /// <summary>
    /// forward/up/right の各ベクトルをログに出力する。
    /// </summary>
    public static void LogAxes(Vector3 forward, Vector3 up, Vector3 right)
    {
        if (!EnableAxisLogging) return;
        Trace.WriteLine($"LookRotation axes F={forward} U={up} R={right}");
    }
}
