using System.Numerics;
using System.Collections.Generic;
using ImGuiNET;
using MiniMikuDance.Util;

namespace MiniMikuDance.UI;

public class UIManager : Singleton<UIManager>
{
    public UIConfig Config { get; private set; } = new();

    private readonly Dictionary<string, bool> _toggleStates = new();

    public float Progress { get; set; }
    public string Message { get; private set; } = string.Empty;
    public bool IsRecording { get; set; }

    public event Action<string>? OnButtonPressed;

    public void LoadConfig(string path)
    {
        Config = JSONUtil.Load<UIConfig>(path);
        _toggleStates.Clear();
        foreach (var t in Config.Toggles)
        {
            _toggleStates[t.Id] = t.DefaultValue;
        }
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

    public void Render()
    {
        foreach (var btn in Config.Buttons)
        {
            if (ImGui.Button(btn.Label))
            {
                OnButtonPressed?.Invoke(btn.Message);
            }
        }

        foreach (var toggle in Config.Toggles)
        {
            bool value = GetToggle(toggle.Id);
            if (ImGui.Checkbox(toggle.Label, ref value))
            {
                _toggleStates[toggle.Id] = value;
            }
        }

        if (Config.ShowProgressBar)
        {
            ImGui.ProgressBar(Progress, new Vector2(-1, 0), string.Empty);
        }

        if (Config.ShowMessage && !string.IsNullOrEmpty(Message))
        {
            ImGui.TextWrapped(Message);
        }

        if (Config.ShowRecordingIndicator && IsRecording)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "● REC");
        }
    }
}
