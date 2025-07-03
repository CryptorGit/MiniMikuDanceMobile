using System;
using System.Collections.ObjectModel;

namespace MiniMikuDanceMaui;

public static class LogService
{
    public static ObservableCollection<string> Logs { get; } = new();
    public static event Action<string>? LogAdded;

    public static void WriteLine(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Logs.Add(line);
        if (Logs.Count > 500)
            Logs.RemoveAt(0);
        LogAdded?.Invoke(line);
        System.Diagnostics.Debug.WriteLine(line);
    }
}
