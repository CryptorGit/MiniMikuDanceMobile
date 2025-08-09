#if IOS
using System;
using System.Collections.Generic;
using System.IO;
using AVFoundation;
using CoreMedia;
using Foundation;
using UIKit;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDanceMaui;

public class IosFrameExtractor : IVideoFrameExtractor
{
    public Action<float>? OnProgress { get; set; }

    public Task<int> GetFrameCountAsync(string videoPath, int fps)
    {
        return Task.Run(() =>
        {
            var asset = AVAsset.FromUrl(NSUrl.FromFilename(videoPath));
            var duration = asset.Duration;
            var totalSeconds = CMTime.GetSeconds(duration);
            return (int)Math.Ceiling(totalSeconds * fps);
        });
    }

    public async IAsyncEnumerable<Stream> ExtractFrames(string videoPath, int fps, Action<float>? onProgress = null)
    {
        var asset = AVAsset.FromUrl(NSUrl.FromFilename(videoPath));
        var generator = new AVAssetImageGenerator(asset)
        {
            AppliesPreferredTrackTransform = true
        };
        var duration = asset.Duration;
        var totalSeconds = duration.Seconds;
        int frameCount = (int)Math.Floor(totalSeconds * fps);
        var progressCb = onProgress ?? OnProgress;
        for (int i = 0; i < frameCount; i++)
        {
            var time = CMTime.FromSeconds(i / (double)fps, fps);
            NSError? error;
            using var cg = generator.CopyCGImageAtTime(time, out var actual, out error);
            if (error != null)
                continue;
            using var image = UIImage.FromImage(cg);
            using var data = image.AsPNG();
            var ms = new MemoryStream(data.ToArray());
            ms.Position = 0;
            yield return ms;
            if (progressCb != null && frameCount > 0)
            {
                progressCb(Math.Clamp((i + 1) / (float)frameCount, 0f, 1f));
            }
        }
    }
}
#endif
