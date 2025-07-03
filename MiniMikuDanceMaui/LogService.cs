using System;

namespace MiniMikuDanceMaui;

public static class LogService
{
    public static void WriteLine(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        System.Diagnostics.Debug.WriteLine(line);
    }
}
