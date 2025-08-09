#if ANDROID
using System;
using Android.Graphics;
using Android.Media;
using System.Collections.Generic;
using System.IO;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDanceMaui;

/// <summary>
/// MediaMetadataRetriever を用いて動画からフレームを抽出する Android 実装。
/// </summary>
public class AndroidFrameExtractor : IVideoFrameExtractor
{
    public Action<float>? OnProgress { get; set; }

    public Task<int> GetFrameCountAsync(string videoPath, int fps)
    {
        return Task.Run(() =>
        {
            using var retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(videoPath);
            var durStr = retriever.ExtractMetadata(MetadataKey.Duration);
            if (!long.TryParse(durStr, out var durationMs))
                durationMs = 0;
            return (int)Math.Ceiling(durationMs / 1000.0 * fps);
        });
    }

    public async IAsyncEnumerable<Stream> ExtractFrames(string videoPath, int fps, Action<float>? onProgress = null)
    {
        using var retriever = new MediaMetadataRetriever();
        retriever.SetDataSource(videoPath);
        var durStr = retriever.ExtractMetadata(MetadataKey.Duration);
        if (!long.TryParse(durStr, out var durationMs))
            durationMs = 0;

        long interval = 1000 / fps;
        int frameCount = (int)(durationMs / interval);
        long index = 0;
        var progressCb = onProgress ?? OnProgress;
        for (long t = 0; t < durationMs; t += interval)
        {
            using var bmp = retriever.GetFrameAtTime(t * 1000, Option.ClosestSync);
            if (bmp == null) continue;
            var ms = new MemoryStream();
            var format = Bitmap.CompressFormat.Png;
            bmp.Compress(format, 100, ms);
            ms.Position = 0;
            yield return ms;
            index++;
            if (progressCb != null && frameCount > 0)
            {
                progressCb(Math.Clamp(index / (float)frameCount, 0f, 1f));
            }
        }
    }
}
#endif
