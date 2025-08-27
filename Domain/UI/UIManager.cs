using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Buffers;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImGuiNET;
using MiniMikuDance.Util;

namespace MiniMikuDance.UI;

public class UIManager : Singleton<UIManager>
{
    private sealed record UIButton(string Label, string Message);
    private sealed record UIToggle(string Label, string Id, bool DefaultValue);

    private readonly List<UIButton> _buttons = new()
    {
        new("Load Model", "load_model"),
        new("Analyze Video", "analyze_video"),
    };

    private readonly List<UIToggle> _toggles = new();
    private readonly bool _showMessage = true;
    private readonly bool _showThumbnail = true;

    private readonly Dictionary<string, bool> _toggleStates = new();
    public string Message { get; private set; } = string.Empty;
    public bool IsRecording { get; set; }
    private int _thumbnailTexture;
    private Vector2 _thumbnailSize;

    public UIManager()
    {
        foreach (var t in _toggles)
        {
            _toggleStates[t.Id] = t.DefaultValue;
        }
    }

    public delegate int TextureLoaderDelegate(ReadOnlySpan<byte> data, int width, int height);

    public event Action<string>? OnButtonPressed;
    public event Action<string, bool>? OnToggleChanged;
    private TextureLoaderDelegate? _textureLoader;

    public void RegisterTextureLoader(TextureLoaderDelegate loader)
    {
        _textureLoader = loader;
    }

    public void SetMessage(string message)
    {
        Message = message;
    }

    public bool GetToggle(string id)
    {
        return _toggleStates.TryGetValue(id, out var v) && v;
    }

    public void SetToggleState(string id, bool value)
    {
        _toggleStates[id] = value;
    }

    public void SetThumbnail(string path)
    {
        if (_textureLoader == null)
            return;

        if (!File.Exists(path))
            return;

        using var image = Image.Load<Rgba32>(path);
        if (image.DangerousTryGetSinglePixelMemory(out var mem))
        {
            _thumbnailTexture = _textureLoader(MemoryMarshal.AsBytes(mem.Span), image.Width, image.Height);
        }
        else
        {
            int byteLen = image.Width * image.Height * 4;
            var buffer = ArrayPool<byte>.Shared.Rent(byteLen);
            try
            {
                image.CopyPixelDataTo(buffer);
                _thumbnailTexture = _textureLoader(buffer.AsSpan(0, byteLen), image.Width, image.Height);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        _thumbnailSize = new Vector2(image.Width, image.Height);
    }

    public void Render()
    {
        for (int i = 0, count = _buttons.Count; i < count; i++)
        {
            var btn = _buttons[i];
            if (ImGui.Button(btn.Label))
            {
                OnButtonPressed?.Invoke(btn.Message);
            }
        }

        for (int i = 0, count = _toggles.Count; i < count; i++)
        {
            var toggle = _toggles[i];
            bool value = GetToggle(toggle.Id);
            if (ImGui.Checkbox(toggle.Label, ref value))
            {
                _toggleStates[toggle.Id] = value;
                OnToggleChanged?.Invoke(toggle.Id, value);
            }
        }

        if (_showMessage && !string.IsNullOrEmpty(Message))
        {
            ImGui.TextWrapped(Message);
        }

        if (_showThumbnail && _thumbnailTexture != 0)
        {
            ImGui.Image((IntPtr)_thumbnailTexture, _thumbnailSize);
        }
    }
}
