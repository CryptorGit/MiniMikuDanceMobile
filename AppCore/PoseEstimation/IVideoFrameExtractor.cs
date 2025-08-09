using System.Collections.Generic;
using System.IO;

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
    /// 指定動画からフレームをメモリストリームとして抽出する。
    /// </summary>
    /// <param name="videoPath">入力動画パス</param>
    /// <param name="fps">抽出するフレームレート</param>
    /// <param name="onProgress">進捗コールバック</param>
    /// <returns>抽出したフレームストリームの列挙</returns>
    IAsyncEnumerable<Stream> ExtractFrames(string videoPath, int fps, Action<float>? onProgress = null);

    /// <summary>
    /// 動画の総フレーム数を取得する。
    /// </summary>
    /// <param name="videoPath">入力動画パス</param>
    /// <param name="fps">フレームレート</param>
    /// <returns>フレーム数。取得できない場合は0</returns>
    Task<int> GetFrameCountAsync(string videoPath, int fps);
}
