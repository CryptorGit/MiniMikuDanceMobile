using System;
using System.IO;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Buffers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;

namespace MiniMikuDance.Recording;

public class RecorderController : IDisposable
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
    private int _droppedFrames;
    private byte[]? _rowBuffer;

    private const int ChannelCapacity = 60;

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
        _droppedFrames = 0;
        while (_imagePool.TryDequeue(out var img))
        {
            img.Dispose();
        }
        for (int i = 0; i < ChannelCapacity; i++)
        {
            _imagePool.Enqueue(new Image<Rgba32>(_width, _height));
        }
        var options = new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true,
        };
        _frameChannel = Channel.CreateBounded<(Image<Rgba32> Image, string Path)>(options);
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
        if (_droppedFrames > 0)
        {
            string msg = $"Dropped frames:{_droppedFrames}\n";
            File.AppendAllText(_infoPath, msg);
            Console.WriteLine(msg.Trim());
        }
        while (_imagePool.TryDequeue(out var img))
        {
            img.Dispose();
        }
        _frameChannel = null;
        _workerTask = null;
        ReleaseRowBuffer();
        return _savedDir;
    }

    public string ThumbnailPath => _thumbnailPath;

    public bool IsRecording => _recording;

    public Task Capture(byte[] rgba, int width, int height)
    {
        if (!_recording || _frameChannel == null) return Task.CompletedTask;

        if (_width != width || _height != height)
        {
            _width = width;
            _height = height;
            while (_imagePool.TryDequeue(out var oldImage))
            {
                oldImage.Dispose();
            }
            for (int i = 0; i < ChannelCapacity; i++)
            {
                _imagePool.Enqueue(new Image<Rgba32>(_width, _height));
            }
        }

        if (!_imagePool.TryDequeue(out var image))
        {
            Interlocked.Increment(ref _droppedFrames);
            return Task.CompletedTask;
        }

        CopyToImage(rgba, image, width, height);
        string path = Path.Combine(_savedDir, $"frame_{_frameIndex:D04}.png");
        if (!_frameChannel.Writer.TryWrite((image, path)))
        {
            Interlocked.Increment(ref _droppedFrames);
            _imagePool.Enqueue(image);
            return Task.CompletedTask;
        }
        if (_frameIndex == 0)
        {
            _thumbnailPath = path;
        }
        _frameIndex++;
        return Task.CompletedTask;
    }

    private void CopyToImage(byte[] src, Image<Rgba32> image, int width, int height)
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
                if (_rowBuffer == null || _rowBuffer.Length < rowBytes)
                {
                    if (_rowBuffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(_rowBuffer);
                    }
                    _rowBuffer = ArrayPool<byte>.Shared.Rent(rowBytes);
                }
                var tmp = _rowBuffer;
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
            for (int y = 0; y < height; y++)
            {
                var row = image.GetPixelRowSpan(y);
                int offset = (height - 1 - y) * width;
                source.Slice(offset, width).CopyTo(row);
            }
        }
    }

    private void ReleaseRowBuffer()
    {
        if (_rowBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_rowBuffer);
            _rowBuffer = null;
        }
    }

    public void Dispose()
    {
        if (_recording)
        {
            StopRecording().GetAwaiter().GetResult();
        }
        else
        {
            ReleaseRowBuffer();
        }
    }

    public string GetSavedPath() => _savedDir;
}

static class ImageExtensionsCompat
{
    public static Span<Rgba32> GetPixelRowSpan(this Image<Rgba32> image, int y)
    {
        return image.DangerousGetPixelRowMemory(y).Span;
    }
}
