using System;
using System.IO;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading.Channels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace MiniMikuDance.Recording;

public class RecorderController
{
    private bool _recording;
    private string _savedDir = string.Empty;
    private string _infoPath = string.Empty;
    private int _frameIndex;
    private string _thumbnailPath = string.Empty;
    private readonly string _baseDir;
    private readonly ConcurrentQueue<Image<Rgba32>> _imagePool = new();
    private Channel<(Image<Rgba32> Image, string Path)>? _frameChannel;
    private Task? _workerTask;
    private int _width;
    private int _height;

    public RecorderController(string baseDir = "Recordings")
    {
        _baseDir = baseDir;
    }

    public string StartRecording(int width, int height, int fps)
    {
        Directory.CreateDirectory(_baseDir);
        string folder = Path.Combine(_baseDir, $"record_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(folder);
        _savedDir = folder;
        _infoPath = Path.Combine(folder, "info.txt");
        File.WriteAllText(_infoPath, $"Resolution:{width}x{height} FPS:{fps} Started:{DateTime.Now}\n");
        _frameIndex = 0;
        _recording = true;
        _width = width;
        _height = height;
        while (_imagePool.TryDequeue(out var img))
        {
            img.Dispose();
        }
        _frameChannel = Channel.CreateBounded<(Image<Rgba32> Image, string Path)>(60);
        _workerTask = Task.Run(async () =>
        {
            await foreach (var (img, path) in _frameChannel.Reader.ReadAllAsync())
            {
                await img.SaveAsPngAsync(path);
                _imagePool.Enqueue(img);
            }
        });
        return _savedDir;
    }

    public async Task<string> StopRecording()
    {
        if (!_recording)
        {
            return _savedDir;
        }

        _recording = false;
        File.AppendAllText(_infoPath, $"Stopped:{DateTime.Now}\n");
        _frameChannel?.Writer.Complete();
        if (_workerTask != null)
        {
            await _workerTask;
        }
        while (_imagePool.TryDequeue(out var img))
        {
            img.Dispose();
        }
        _frameChannel = null;
        _workerTask = null;
        return _savedDir;
    }

    public string ThumbnailPath => _thumbnailPath;

    public bool IsRecording => _recording;

    public async Task Capture(byte[] rgba, int width, int height)
    {
        if (!_recording || _frameChannel == null) return;

        if (_width != width || _height != height)
        {
            _width = width;
            _height = height;
            while (_imagePool.TryDequeue(out var oldImage))
            {
                oldImage.Dispose();
            }
        }

        if (!_imagePool.TryDequeue(out var image))
        {
            image = new Image<Rgba32>(width, height);
        }

        CopyToImage(rgba, image, width, height);
        string path = Path.Combine(_savedDir, $"frame_{_frameIndex:D04}.png");
        await _frameChannel.Writer.WriteAsync((image, path));
        if (_frameIndex == 0)
        {
            _thumbnailPath = path;
        }
        _frameIndex++;
    }

    private static void CopyToImage(byte[] src, Image<Rgba32> image, int width, int height)
    {
        var source = MemoryMarshal.Cast<byte, Rgba32>(src);

        if (image.DangerousTryGetSinglePixelMemory(out var mem))
        {
            var dest = mem.Span;
            source.CopyTo(dest);

            if (height > 1)
            {
                var destBytes = MemoryMarshal.AsBytes(dest);
                int rowBytes = width * 4;
                Span<byte> tmp = stackalloc byte[rowBytes];

                unsafe
                {
                    fixed (byte* destPtr = destBytes)
                    fixed (byte* tmpPtr = tmp)
                    {
                        for (int y = 0; y < height / 2; y++)
                        {
                            byte* top = destPtr + y * rowBytes;
                            byte* bottom = destPtr + (height - 1 - y) * rowBytes;
                            Buffer.MemoryCopy(top, tmpPtr, rowBytes, rowBytes);
                            Buffer.MemoryCopy(bottom, top, rowBytes, rowBytes);
                            Buffer.MemoryCopy(tmpPtr, bottom, rowBytes, rowBytes);
                        }
                    }
                }
            }
        }
        else
        {
            image.ProcessPixelRows(accessor =>
            {
                var srcSpan = MemoryMarshal.Cast<byte, Rgba32>(src);
                int offset = (height - 1) * width;
                for (int y = 0; y < height; y++)
                {
                    srcSpan.Slice(offset, width).CopyTo(accessor.GetRowSpan(y));
                    offset -= width;
                }
            });
        }
    }

    public string GetSavedPath() => _savedDir;
}
