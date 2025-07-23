#if IOS
using AVFoundation;
using CoreMedia;
using Foundation;
using UIKit;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDanceMaui;

public class IosFrameExtractor : IVideoFrameExtractor
{
    public Task<string[]> ExtractFramesAsync(string videoPath, string outputDir, int fps)
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
            }
            return result.ToArray();
        });
    }
}
#endif
