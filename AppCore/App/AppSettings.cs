using MiniMikuDance.Util;
using MiniMikuDance.Physics;
using System.Numerics;

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

    /// <summary>PMXモデルに適用するスケール。</summary>
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

    /// <summary>物理設定。</summary>
    public PhysicsConfig Physics { get; set; } =
        new(new Vector3(0f, -9.81f, 0f), 8, 1);


    private const string DefaultFile = "Configs/appsettings.json";

    /// <summary>
    /// 設定ファイルを読み込む。存在しない場合はデフォルト値で生成する。
    /// </summary>
    public static AppSettings Load(string path = DefaultFile)
    {
        return JSONUtil.Load<AppSettings>(path);
    }

    /// <summary>
    /// 現在の設定内容を保存する。
    /// </summary>
    public void Save(string path = DefaultFile)
    {
        JSONUtil.Save(path, this);
    }
}
