using System;
using System.IO;
using System.Threading.Tasks;
using MiniMikuDanceMaui.Storage;
#if ANDROID
using Android.Runtime;
#endif
#if IOS
using ObjCRuntime;
#endif

namespace MiniMikuDanceMaui.Helpers;

public static class CrashLogger
{
    static bool _globalRegistered;

    public static void RegisterGlobal()
    {
        if (_globalRegistered)
            return;

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Log(ex);
            else
                Log(new Exception(e.ExceptionObject?.ToString() ?? "Unknown exception"));
        };
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Log(e.Exception);
            e.SetObserved();
        };
        _globalRegistered = true;
    }

    public static void Log(Exception ex)
    {
        try
        {
            var dir = MmdFileSystem.Ensure("logs");
            var path = Path.Combine(dir, "crash.log");
            File.AppendAllText(path, $"{DateTime.UtcNow:O} {ex}\n");
        }
        catch
        {
        }
    }

#if ANDROID
    static bool _androidRegistered;
    public static void RegisterAndroid()
    {
        if (_androidRegistered)
            return;

        AndroidEnvironment.UnhandledExceptionRaiser += (sender, e) =>
        {
            Log(e.Exception);
            e.Handled = true;
        };
        _androidRegistered = true;
    }
#endif

#if IOS
    static bool _iosRegistered;
    public static void RegisteriOS()
    {
        if (_iosRegistered)
            return;

        Runtime.MarshalObjectiveCException += (sender, e) =>
        {
            Log(e.Exception);
        };
        _iosRegistered = true;
    }
#endif
}

