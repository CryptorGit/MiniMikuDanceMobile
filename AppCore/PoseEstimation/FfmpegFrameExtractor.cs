using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MiniMikuDance.PoseEstimation;
public class FfmpegFrameExtractor : IVideoFrameExtractor
{
    public Action<float>? OnProgress { get; set; }

    private static string? _cachedFfmpegPath;

    /// <summary>
    /// キャッシュしているffmpegパスを再探索します。
    /// </summary>
    public static void ResetFfmpegPath() => _cachedFfmpegPath = null;

    /// <summary>
    /// キャッシュされたffmpegパスの存在を検証します。
    /// </summary>
    public static bool RevalidateFfmpegPath()
    {
        if (string.IsNullOrEmpty(_cachedFfmpegPath) || !File.Exists(_cachedFfmpegPath))
        {
            _cachedFfmpegPath = null;
            return false;
        }
        return true;
    }

    public async Task<int> GetFrameCountAsync(string videoPath, int fps)
    {
        var ffmpeg = FindFfmpeg();
        if (ffmpeg == null)
            return 0;
        var duration = await GetDurationAsync(ffmpeg, videoPath);
        return duration > TimeSpan.Zero ? (int)Math.Ceiling(duration.TotalSeconds * fps) : 0;
    }
    private static string? FindFfmpeg()
    {
        if (!string.IsNullOrEmpty(_cachedFfmpegPath) && File.Exists(_cachedFfmpegPath))
            return _cachedFfmpegPath;

        var env = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        if (!string.IsNullOrEmpty(env) && File.Exists(env))
        {
            _cachedFfmpegPath = env;
            return _cachedFfmpegPath;
        }

        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator);
        foreach (var p in paths)
        {
            var exe = Path.Combine(p, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
            if (File.Exists(exe))
            {
                _cachedFfmpegPath = exe;
                return _cachedFfmpegPath;
            }
        }
        _cachedFfmpegPath = null;
        return null;
    }


    public async IAsyncEnumerable<Stream> ExtractFrames(string videoPath, int fps, Action<float>? onProgress = null)
    {
        var ffmpeg = FindFfmpeg();
        if (ffmpeg == null)
        {
            throw new FileNotFoundException("ffmpeg not found. Please install ffmpeg or bundle it with the app.");
        }

        var progressCb = onProgress ?? OnProgress;

        var duration = await GetDurationAsync(ffmpeg, videoPath);
        int totalFrames = duration > TimeSpan.Zero ? (int)Math.Ceiling(duration.TotalSeconds * fps) : -1;

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = $"-i \"{videoPath}\" -vf fps={fps} -f image2pipe -vcodec png -hide_banner -loglevel error -",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(startInfo);
        if (proc == null)
            throw new InvalidOperationException("Failed to start ffmpeg process.");

        var errTask = proc.StandardError.ReadToEndAsync();
        var output = proc.StandardOutput.BaseStream;
        var buffer = new byte[4096];
        var current = new MemoryStream();
        var pngEnd = new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 };
        int match = 0;
        int index = 0;

        while (true)
        {
            int read = await output.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (read <= 0) break;
            for (int i = 0; i < read; i++)
            {
                byte b = buffer[i];
                current.WriteByte(b);
                if (b == pngEnd[match])
                {
                    match++;
                    if (match == pngEnd.Length)
                    {
                        current.Position = 0;
                        yield return current;
                        current = new MemoryStream();
                        match = 0;
                        index++;
                        if (progressCb != null && totalFrames > 0)
                        {
                            progressCb(Math.Clamp(index / (float)totalFrames, 0f, 1f));
                        }
                    }
                }
                else
                {
                    match = b == pngEnd[0] ? 1 : 0;
                }
            }
        }

        current.Dispose();
        await proc.WaitForExitAsync();
        var err = await errTask;
        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"ffmpeg failed: {err}");
        }
    }

    private static async Task<TimeSpan> GetDurationAsync(string ffmpeg, string path)
    {
        var info = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = $"-i \"{path}\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(info);
        if (p == null)
            return TimeSpan.Zero;
        string err = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        var idx = err.IndexOf("Duration:");
        if (idx >= 0)
        {
            var sub = err.Substring(idx + 9, 11);
            if (TimeSpan.TryParse(sub, out var dur))
                return dur;
        }
        return TimeSpan.Zero;
    }
}
