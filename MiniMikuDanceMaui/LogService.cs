using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MiniMikuDanceMaui;

public static class LogService
{
    public static event Action<string>? LineLogged;

    private const int MaxHistory = 1000;
    private static readonly List<string> _history = new();
    private static readonly object _historyLock = new();
    private static readonly string _logFilePath;
    private static readonly string _logDirectory;
    private static readonly string _terminalLogFilePath;

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
            if (_history.Count > MaxHistory)
            {
                _history.RemoveAt(0);
            }
        }
        LineLogged?.Invoke(line);
        Task.Run(() =>
        {
            try
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
            try
            {
                File.AppendAllText(_terminalLogFilePath, line + Environment.NewLine);
            }
            catch
            {
                // ignore logging failures to external file
            }
        });
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
