using System;

namespace MiniMikuDanceMaui;

public static class LogService
{
    public static event Action<string>? LineLogged;

    private static readonly List<string> _history = new();
    public static IReadOnlyList<string> History => _history.AsReadOnly();

    private static void AddLine(string line)
    {
        _history.Add(line);
        LineLogged?.Invoke(line);
    }

    public static void WriteLine(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        AddLine(line);
    }

    internal static void AddExternalLine(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        AddLine(line);
    }
}
