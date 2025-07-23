using System.Diagnostics;

namespace MiniMikuDance.PoseEstimation;
public class FfmpegFrameExtractor : IVideoFrameExtractor
{
    private static string? FindFfmpeg()
    {
        var env = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        if (!string.IsNullOrEmpty(env) && File.Exists(env))
        {
            return env;
        }

        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator);
        foreach (var p in paths)
        {
            var exe = Path.Combine(p, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
            if (File.Exists(exe))
                return exe;
        }
        return null;
    }

    public async Task<string[]> ExtractFrames(string videoPath, int fps, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var ffmpeg = FindFfmpeg();
        if (ffmpeg == null)
        {
            throw new FileNotFoundException("ffmpeg not found. Please install ffmpeg or bundle it with the app.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = $"-i \"{videoPath}\" -vf fps={fps} \"{Path.Combine(outputDir, "frame_%08d.png")}\" -hide_banner -loglevel error",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(startInfo);
        if (proc == null)
            throw new InvalidOperationException("Failed to start ffmpeg process.");

        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
        {
            var err = await proc.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"ffmpeg failed: {err}");
        }
        var files = Directory.GetFiles(outputDir, "frame_*.png");
        Array.Sort(files);
        return files;
    }
}
