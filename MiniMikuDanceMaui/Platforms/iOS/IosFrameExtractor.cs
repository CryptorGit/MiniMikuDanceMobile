#if IOS
using System;
using AVFoundation;
using CoreMedia;
using Foundation;
using UIKit;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDanceMaui;

public class IosFrameExtractor : IVideoFrameExtractor
{
    public Action<float>? OnProgress { get; set; }

    public Task<string[]> ExtractFrames(string videoPath, int fps, string outputDir, Action<float>? onProgress = null)
    {
        return Task.Run(() =>
        {
            Directory.CreateDirectory(outputDir);
            var asset = AVAsset.FromUrl(NSUrl.FromFilename(videoPath));
            var generator = new AVAssetImageGenerator(asset)
            {
                AppliesPreferredTrackTransform = true
            };
            var duration = asset.Duration;
            var totalSeconds = duration.Seconds;
            int frameCount = (int)Math.Floor(totalSeconds * fps);
            var progressCb = onProgress ?? OnProgress;
            var result = new List<string>(frameCount);
            for (int i = 0; i < frameCount; i++)
            {
                var time = CMTime.FromSeconds(i / (double)fps, fps);
                NSError? error;
                using var cg = generator.CopyCGImageAtTime(time, out var actual, out error);
                if (error != null)
                    continue;
                using var image = UIImage.FromImage(cg);
                var path = Path.Combine(outputDir, $"frame_{i:D08}.png");
                using var data = image.AsPNG();
                File.WriteAllBytes(path, data.ToArray());
                result.Add(path);
                if (progressCb != null && frameCount > 0)
                {
                    progressCb(Math.Clamp((i + 1) / (float)frameCount, 0f, 1f));
                }
            }
            return result.ToArray();
        });
    }
}
#endif
