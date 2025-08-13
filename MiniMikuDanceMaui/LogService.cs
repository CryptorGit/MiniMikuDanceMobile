using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MiniMikuDanceMaui;

public static class LogService
{
    public static event Action<string>? LineLogged;

    private const int MaxHistory = 1000;
    private const int LogChannelCapacity = 100;
    private static readonly Queue<string> _history = new();
    private static readonly object _historyLock = new();
    private static readonly string _logFilePath;
    private static readonly string _logDirectory;
    private static readonly string _terminalLogFilePath;
    private static readonly StreamWriter _logWriter;
    private static readonly StreamWriter _terminalLogWriter;
    private static readonly Task _processingTask;
    private static bool _isShutdown;
    private static readonly Channel<string> _logChannel = Channel.CreateBounded<string>(
        new BoundedChannelOptions(LogChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
    private static StringBuilder? _builder;

    static LogService()
    {
        _logDirectory = Path.Combine(FileSystem.AppDataDirectory, "MikuMikuDance", "data", "Log");
        Directory.CreateDirectory(_logDirectory);
        _logFilePath = Path.Combine(_logDirectory, "log.txt");

        var externalDir = MmdFileSystem.Ensure("Log");
        _terminalLogFilePath = Path.Combine(externalDir, "tarminalLog.txt");

        _logWriter = new StreamWriter(_logFilePath, append: true, Encoding.UTF8);
        _terminalLogWriter = new StreamWriter(_terminalLogFilePath, append: true, Encoding.UTF8);

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();

        _processingTask = Task.Run(ProcessLogQueue);
    }

    public static IReadOnlyList<string> History
    {
        get
        {
            lock (_historyLock)
            {
                return _history.Reverse().ToArray();
            }
        }
    }

    private static void AddLine(string line)
    {
        lock (_historyLock)
        {
            _history.Enqueue(line);
            if (_history.Count > MaxHistory)
            {
                _history.Dequeue();
            }
        }
        LineLogged?.Invoke(line);
        _logChannel.Writer.TryWrite(line);
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

    public static void Shutdown()
    {
        if (_isShutdown)
        {
            return;
        }

        _isShutdown = true;
        _logChannel.Writer.TryComplete();
        try
        {
            _processingTask.Wait();
        }
        catch
        {
            // ignore exceptions during shutdown
        }
        _builder = null;
    }

    private static async Task ProcessLogQueue()
    {
        var builder = _builder ??= new StringBuilder();

        while (await _logChannel.Reader.WaitToReadAsync())
        {
            builder.Clear();
            while (_logChannel.Reader.TryRead(out var line))
            {
                builder.AppendLine(line);
            }

            string text = builder.ToString();
            if (text.Length == 0)
            {
                continue;
            }

            try
            {
                await _logWriter.WriteAsync(text);
                await _logWriter.FlushAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
            try
            {
                await _terminalLogWriter.WriteAsync(text);
                await _terminalLogWriter.FlushAsync();
            }
            catch
            {
                // ignore logging failures to external file
            }
        }

        _logWriter.Dispose();
        _terminalLogWriter.Dispose();
    }
}
