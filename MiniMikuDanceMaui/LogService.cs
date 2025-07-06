using System;

namespace MiniMikuDanceMaui;

public static class LogService
{
    public static event Action<string>? LineLogged;

    private static readonly List<string> _history = new();
    public static IReadOnlyList<string> History => _history.AsReadOnly();

    public static void WriteLine(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        _history.Add(line);
        LineLogged?.Invoke(line);
    }
}
