using MiniMikuDance.Util;
using MiniMikuDance.Physics;
using System;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiniMikuDance.App;

/// <summary>
/// アプリ全体のユーザー設定を管理するクラス。
/// JSON ファイルに保存・読み込みを行う。
/// </summary>
public class AppSettings
{
    /// <summary>最後に使用したモデルファイルのパス。</summary>
    public string LastModelPath { get; set; } = string.Empty;

    /// <summary>最後に解析した動画ファイルのパス。</summary>
    public string LastVideoPath { get; set; } = string.Empty;

    /// <summary>PMXモデルのスケールのデフォルト値。</summary>
    public const float DefaultModelScale = 1.0f;

    /// <summary>PMXモデルに適用するスケール。質量や重力などの物理パラメータは自動的にスケーリングされないため、
    /// 1 単位 = 1 メートルに合わせてモデル側を調整する。</summary>
    public float ModelScale { get; set; } = DefaultModelScale;

    /// <summary>ステージの半径のデフォルト値。</summary>
    public const float DefaultStageSize = 30f;

    /// <summary>ステージの半径。</summary>
    public float StageSize { get; set; } = DefaultStageSize;

    /// <summary>カメラ距離のデフォルト値。</summary>
    public const float DefaultCameraDistance = 4f;

    /// <summary>カメラ距離。</summary>
    public float CameraDistance { get; set; } = DefaultCameraDistance;

    /// <summary>カメラの注視点Y座標のデフォルト値。</summary>
    public const float DefaultCameraTargetY = 0.5f;

    /// <summary>カメラの注視点Y座標。</summary>
    public float CameraTargetY { get; set; } = DefaultCameraTargetY;

    /// <summary>ボーン選択のピクセル閾値のデフォルト値。</summary>
    public const float DefaultBonePickPixels = 60f;

    /// <summary>ボーン選択時のピクセル閾値。</summary>
    public float BonePickPixels { get; set; } = DefaultBonePickPixels;

    /// <summary>IKボーン球サイズのデフォルト値 (0.001f)。</summary>
    public const float DefaultIkBoneScale = 0.001f;

    /// <summary>IKボーン球サイズ。</summary>
    public float IkBoneScale { get; set; } = DefaultIkBoneScale;

    /// <summary>テクスチャキャッシュ数のデフォルト値。</summary>
    public const int DefaultTextureCacheSize = 64;

    /// <summary>テクスチャキャッシュの最大数。</summary>
    public int TextureCacheSize { get; set; } = DefaultTextureCacheSize;

    /// <summary>スフィアマップの強度のデフォルト値。</summary>
    public const float DefaultSphereStrength = 1f;

    /// <summary>スフィアマップの強度。</summary>
    public float SphereStrength { get; set; } = DefaultSphereStrength;

    /// <summary>トゥーンマップの強度のデフォルト値。</summary>
    public const float DefaultToonStrength = 0f;

    /// <summary>トゥーンマップの強度。</summary>
    public float ToonStrength { get; set; } = DefaultToonStrength;

    /// <summary>ボーン種別で色や形状を区別するか。</summary>
    public bool DistinguishBoneTypes { get; set; }
        = false;

    /// <summary>物理演算を有効にするかの既定値。</summary>
    public const bool DefaultEnablePhysics = false;

    /// <summary>物理演算を有効にするか。</summary>
    public bool EnablePhysics { get; set; } = DefaultEnablePhysics;

    /// <summary>物理設定。</summary>
    public PhysicsConfig Physics { get; set; } =
        new(new Vector3(0f, -9.81f, 0f), 8, 1, 0.98f, 0.5f, 0f, 0.2f, 0.5f, false);


    private const string DefaultFile = "Configs/appsettings.json";

    /// <summary>
    /// 設定ファイルを読み込む。存在しない場合はデフォルト値で生成する。
    /// 読み込んだ Physics.Gravity の値をログ出力する。
    /// </summary>
    public static AppSettings Load(string path = DefaultFile, ILogger<AppSettings>? logger = null)
    {
        var result = JSONUtil.Load<AppSettings>(path);
        var log = logger ?? NullLogger<AppSettings>.Instance;
        var physics = result.Physics;
        var gravity = physics.Gravity;
        bool IsInvalid(float v) => float.IsNaN(v) || float.IsInfinity(v) || v < -1000f || v > 1000f;
        bool IsNearZero(float v) => MathF.Abs(v) < 1e-3f;
        if (IsInvalid(gravity.X) || IsInvalid(gravity.Y) || IsInvalid(gravity.Z))
        {
            log.LogWarning("Invalid Physics.Gravity detected: {Gravity}. Resetting to default.", gravity);
            physics.Gravity = new Vector3(0f, -9.81f, 0f);
            result.Physics = physics;
            result.Save(path);
        }
        else if (IsNearZero(gravity.X) && IsNearZero(gravity.Y) && IsNearZero(gravity.Z))
        {
            log.LogWarning("Physics.Gravity magnitude too small: {Gravity}. Resetting to default.", gravity);
            physics.Gravity = new Vector3(0f, -9.81f, 0f);
            result.Physics = physics;
            result.Save(path);
        }
        else
        {
            log.LogInformation("Loaded Physics.Gravity: {Gravity}", gravity);
        }
        return result;
    }

    /// <summary>
    /// 現在の設定内容を保存する。
    /// </summary>
    public void Save(string path = DefaultFile)
    {
        JSONUtil.Save(path, this);
    }
}
