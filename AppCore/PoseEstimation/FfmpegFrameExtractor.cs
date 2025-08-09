using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace MiniMikuDance.PoseEstimation;
public class FfmpegFrameExtractor : IVideoFrameExtractor
{
    public Action<float>? OnProgress { get; set; }

    private static string? _cachedFfmpegPath;
    private static readonly Dictionary<string, (TimeSpan Duration, DateTime CachedAt)> _durationCache = new();
    private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(1);

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
        return await GetFrameCountInternalAsync(ffmpeg, videoPath, fps);
    }

    private static async Task<int> GetFrameCountInternalAsync(string ffmpeg, string videoPath, int fps)
    {
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

    public async Task<string[]> ExtractFrames(string videoPath, int fps, string outputDir, Action<float>? onProgress = null)
    {
        Directory.CreateDirectory(outputDir);
        var ffmpeg = FindFfmpeg();
        if (ffmpeg == null)
        {
            throw new FileNotFoundException("ffmpeg not found. Please install ffmpeg or bundle it with the app.");
        }

        var progressCb = onProgress ?? OnProgress;

        // フレーム数計算
        int totalFrames = await GetFrameCountInternalAsync(ffmpeg, videoPath, fps);

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = $"-i \"{videoPath}\" -vf fps={fps} \"{Path.Combine(outputDir, "frame_%08d.png")}\" -hide_banner -loglevel error -progress pipe:1 -nostats",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(startInfo);
        if (proc == null)
            throw new InvalidOperationException("Failed to start ffmpeg process.");

        var progressTask = Task.Run(async () =>
        {
            string? line;
            while ((line = await proc.StandardOutput.ReadLineAsync()) != null)
            {
                if (line.StartsWith("frame=") && int.TryParse(line.AsSpan(6), out var f))
                {
                    if (totalFrames > 0 && progressCb != null)
                    {
                        progressCb(Math.Clamp(f / (float)totalFrames, 0f, 1f));
                    }
                }
            }
        });
        // プロセス終了後に残りの出力を読み取るため、
        // 先にプロセスの終了を待機し、その後でprogressTaskを待機する。
        await proc.WaitForExitAsync();
        await progressTask;
        if (proc.ExitCode != 0)
        {
            var err = await proc.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"ffmpeg failed: {err}");
        }
        var files = Directory.GetFiles(outputDir, "frame_*.png");
        Array.Sort(files);
        return files;
    }

    private static async Task<TimeSpan> GetDurationAsync(string ffmpeg, string path)
    {
        try
        {
            if (_durationCache.TryGetValue(path, out var cache) && DateTime.UtcNow - cache.CachedAt < _cacheLifetime)
            {
                return cache.Duration;
            }

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
                {
                    _durationCache[path] = (dur, DateTime.UtcNow);
                    return dur;
                }
            }
        }
        catch
        {
            // 既存機能との互換性を保つため、例外は呼び出し元に伝播させない
        }
        _durationCache.Remove(path);
        return TimeSpan.Zero;
    }
}
