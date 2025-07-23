namespace MiniMikuDance.PoseEstimation;

/// <summary>
/// 動画からフレーム画像を抽出するためのインターフェース。
/// </summary>
public interface IVideoFrameExtractor
{
    /// <summary>
    /// 進捗を通知するためのコールバック。
    /// </summary>
    Action<float>? OnProgress { get; set; }
    /// <summary>
    /// 指定動画からフレームを抽出し outputDir に保存する。
    /// </summary>
    /// <param name="videoPath">入力動画パス</param>
    /// <param name="fps">抽出するフレームレート</param>
    /// <param name="outputDir">出力ディレクトリ</param>
    /// <returns>保存したフレーム画像のパス配列</returns>
    Task<string[]> ExtractFrames(string videoPath, int fps, string outputDir, Action<float>? onProgress = null);
}
