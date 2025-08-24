using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace MiniMikuDanceMaui;

internal static class AppLogger
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "log.txt");
        builder.AddProvider(new SimpleFileLoggerProvider(path));
        builder.SetMinimumLevel(LogLevel.Error);
    });

    public static ILogger<T> Create<T>() => Factory.CreateLogger<T>();

    public static ILogger Create(string categoryName) => Factory.CreateLogger(categoryName);
}
