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
    public UIConfig Config { get; private set; } = new();

    private readonly Dictionary<string, bool> _toggleStates = new();

    public float Progress { get; set; }
    public float ExtractProgress { get; set; }
    public string Message { get; private set; } = string.Empty;
    public bool IsRecording { get; set; }
    private int _thumbnailTexture;
    private Vector2 _thumbnailSize;

    public delegate int TextureLoaderDelegate(ReadOnlySpan<byte> data, int width, int height);

    public event Action<string>? OnButtonPressed;
    public event Action<string, bool>? OnToggleChanged;
    private TextureLoaderDelegate? _textureLoader;

    public void LoadConfig(string path)
    {
        Config = JSONUtil.Load<UIConfig>(path);
        _toggleStates.Clear();
        foreach (var t in Config.Toggles)
        {
            _toggleStates[t.Id] = t.DefaultValue;
        }
    }

    public void LoadConfig(UIConfig config)
    {
        Config = config;
        _toggleStates.Clear();
        foreach (var t in Config.Toggles)
        {
            _toggleStates[t.Id] = t.DefaultValue;
        }
    }

    public void RegisterTextureLoader(TextureLoaderDelegate loader)
    {
        _textureLoader = loader;
    }

    public void SaveConfig(string path)
    {
        JSONUtil.Save(path, Config);
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
        if (!File.Exists(path) || _textureLoader == null)
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
        for (int i = 0, count = Config.Buttons.Count; i < count; i++)
        {
            var btn = Config.Buttons[i];
            if (ImGui.Button(btn.Label))
            {
                OnButtonPressed?.Invoke(btn.Message);
            }
        }

        for (int i = 0, count = Config.Toggles.Count; i < count; i++)
        {
            var toggle = Config.Toggles[i];
            bool value = GetToggle(toggle.Id);
            if (ImGui.Checkbox(toggle.Label, ref value))
            {
                _toggleStates[toggle.Id] = value;
                OnToggleChanged?.Invoke(toggle.Id, value);
            }
        }

        if (Config.ShowProgressBar)
        {
            ImGui.ProgressBar(ExtractProgress, new Vector2(-1, 0), string.Empty);
        }

        if (Config.ShowMessage && !string.IsNullOrEmpty(Message))
        {
            ImGui.TextWrapped(Message);
        }

        if (Config.ShowThumbnail && _thumbnailTexture != 0)
        {
            ImGui.Image((IntPtr)_thumbnailTexture, _thumbnailSize);
        }
    }
}
