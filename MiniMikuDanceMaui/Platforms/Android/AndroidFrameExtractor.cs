#if ANDROID
using Android.Graphics;
using Android.Media;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDanceMaui;

/// <summary>
/// MediaMetadataRetriever を用いて動画からフレームを抽出する Android 実装。
/// </summary>
public class AndroidFrameExtractor : IVideoFrameExtractor
{
    public Task<string[]> ExtractFrames(string videoPath, int fps, string outputDir)
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
            for (long t = 0; t < durationMs; t += interval)
            {
                using var bmp = retriever.GetFrameAtTime(t * 1000, Option.ClosestSync);
                if (bmp == null) continue;
                string path = Path.Combine(outputDir, $"frame_{index:D08}.png");
                using var fs = File.OpenWrite(path);
                bmp.Compress(Bitmap.CompressFormat.Png, 100, fs);
                list.Add(path);
                index++;
            }
            return list.ToArray();
        });
    }
}
#endif
