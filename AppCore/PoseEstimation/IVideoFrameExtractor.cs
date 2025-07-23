namespace MiniMikuDance.PoseEstimation;

public interface IVideoFrameExtractor
{
    Task<string[]> ExtractFramesAsync(string videoPath, string outputDir, int fps);
}
