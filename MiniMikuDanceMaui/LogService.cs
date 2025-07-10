using System;
using System.IO;

namespace MiniMikuDanceMaui;

public static class LogService
{
    public static event Action<string>? LineLogged;

    private static readonly List<string> _history = new();
    private static readonly object _historyLock = new();
    private static readonly string _logFilePath;
    private static readonly string _logDirectory;

    static LogService()
    {
        _logDirectory = Path.Combine(FileSystem.AppDataDirectory, "Log");
        Directory.CreateDirectory(_logDirectory);
        _logFilePath = Path.Combine(_logDirectory, "DebugLog.txt");
    }

    public static IReadOnlyList<string> History
    {
        get
        {
            lock (_historyLock)
            {
                return _history.ToArray();
            }
        }
    }

    private static void AddLine(string line)
    {
        lock (_historyLock)
        {
            _history.Add(line);
            try
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
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
