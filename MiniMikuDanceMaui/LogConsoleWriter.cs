using System.IO;
using System.Text;

namespace MiniMikuDanceMaui;

public class LogConsoleWriter : TextWriter
{
    private readonly TextWriter _original;

    public LogConsoleWriter(TextWriter original)
    {
        _original = original;
    }

    public override Encoding Encoding => _original.Encoding;

    public override void Write(char value)
    {
        _original.Write(value);
    }

    public override void Write(string? value)
    {
        _original.Write(value);
    }

    public override void WriteLine(string? value)
    {
        _original.WriteLine(value);
        if (!string.IsNullOrEmpty(value))
            LogService.AddExternalLine(value, LogService.LogLevel.Debug);
    }
}
