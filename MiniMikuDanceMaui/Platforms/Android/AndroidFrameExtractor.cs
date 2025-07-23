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

    public Task<string[]> ExtractFrames(string videoPath, int fps, string outputDir, Action<float>? onProgress = null)
    {
        return Task.Run(() =>
        {
            Directory.CreateDirectory(outputDir);
            using var retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(videoPath);
            var durStr = retriever.ExtractMetadata(MetadataKey.Duration);
            if (!long.TryParse(durStr, out var durationMs))
                durationMs = 0;

            long interval = 1000 / fps;
            var list = new List<string>();
            long index = 0;
            int frameCount = (int)(durationMs / interval);
            var progressCb = onProgress ?? OnProgress;
            for (long t = 0; t < durationMs; t += interval)
            {
                using var bmp = retriever.GetFrameAtTime(t * 1000, Option.ClosestSync);
                if (bmp == null) continue;
                string path = System.IO.Path.Combine(outputDir, $"frame_{index:D08}.png");
                using var fs = File.OpenWrite(path);
                bmp.Compress(Bitmap.CompressFormat.Png, 100, fs);
                list.Add(path);
                index++;
                if (progressCb != null && frameCount > 0)
                {
                    progressCb(Math.Clamp(index / (float)frameCount, 0f, 1f));
                }
            }
            return list.ToArray();
        });
    }
}
#endif
