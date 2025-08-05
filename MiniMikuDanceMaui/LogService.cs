using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Maui.Storage;

namespace MiniMikuDanceMaui;

public static class LogService
{
    public static event Action<string>? LineLogged;

    private static readonly List<string> _history = new();
    private static readonly object _historyLock = new();
    private static readonly string _logFilePath;
    private static readonly string _logDirectory;
    private static readonly string _terminalLogFilePath;

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static LogLevel MinimumLevel { get; set; } = LogLevel.Info;

    static LogService()
    {
        _logDirectory = Path.Combine(FileSystem.AppDataDirectory, "MikuMikuDance", "data", "Log");
        Directory.CreateDirectory(_logDirectory);
        _logFilePath = Path.Combine(_logDirectory, "log.txt");

        var externalDir = MmdFileSystem.Ensure("Log");
        _terminalLogFilePath = Path.Combine(externalDir, "tarminalLog.txt");
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
            catch
            {
            }
            try
            {
                File.AppendAllText(_terminalLogFilePath, line + Environment.NewLine);
            }
            catch
            {
                // ignore logging failures to external file
            }
        }
        LineLogged?.Invoke(line);
    }

    public static void WriteLine(string message, LogLevel level = LogLevel.Info)
    {
        if (level < MinimumLevel) return;
        string line = $"[{DateTime.Now:HH:mm:ss}][{level}] {message}";
        AddLine(line);
    }

    internal static void AddExternalLine(string message, LogLevel level = LogLevel.Info)
    {
        if (level < MinimumLevel) return;
        string line = $"[{DateTime.Now:HH:mm:ss}][{level}] {message}";
        AddLine(line);
    }
}
