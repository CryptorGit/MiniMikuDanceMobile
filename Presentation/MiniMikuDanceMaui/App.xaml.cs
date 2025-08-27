using Microsoft.Maui.Controls;
using MiniMikuDance.Domain.UseCases;
using MiniMikuDance.Data.Repositories;
using System;
using System.IO;

namespace MiniMikuDanceMaui;

public partial class App : Application, IDisposable
{
    public static AppInitializer Initializer { get; } = new();

    public App()
    {
        InitializeComponent();

        Directory.SetCurrentDirectory(MmdFileSystem.BaseDir);
        Initializer.SettingsRepository = SettingsRepository.Instance;
        Initializer.Initialize(null, MmdFileSystem.BaseDir);

        var baseDir = AppContext.BaseDirectory;
        var crashLog = Path.Combine(baseDir, "crash_log.txt");
        if (!File.Exists(crashLog)) File.WriteAllText(crashLog, string.Empty);
        var logFile = Path.Combine(baseDir, "log.txt");
        if (!File.Exists(logFile)) File.WriteAllText(logFile, string.Empty);
    }

    protected override Window CreateWindow(Microsoft.Maui.IActivationState? activationState)
    {
        return new Window(new NavigationPage(new MainPage()));
    }

    public void Dispose()
    {
        Initializer.Dispose();
    }
}
