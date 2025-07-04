using System;
using System.Diagnostics;

namespace MiniMikuDanceMaui;

public class LogTraceListener : TraceListener
{
    public override void Write(string? message) { }
    public override void WriteLine(string? message)
    {
        if (!string.IsNullOrEmpty(message))
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}
