using System.IO;
using SystemPath = System.IO.Path;
using Microsoft.Maui.Storage;
#if ANDROID
using AndroidX.DocumentFile.Provider;
using Android.App;
#endif

namespace MiniMikuDanceMaui;

public static class MmdFileSystem
{
    public static string BaseDir { get; private set; } = string.Empty;
    public static string WorkDir { get; private set; } = string.Empty;
#if ANDROID
    public static Android.Net.Uri? BaseUri { get; private set; }
#endif

    static MmdFileSystem()
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
        {
            var uri = Preferences.Get("saf_base_uri", null);
            if (!string.IsNullOrEmpty(uri))
            {
                BaseUri = Android.Net.Uri.Parse(uri);
                LogService.WriteLine($"[MmdFileSystem] Using SAF storage: {uri}", LogService.LogLevel.Info);
                var root = FileSystem.AppDataDirectory;
                WorkDir = SystemPath.Combine(root, "MiniMikuDance", "data");
                Directory.CreateDirectory(WorkDir);
                Directory.CreateDirectory(SystemPath.Combine(WorkDir, "Movie"));
                return;
            }
        }
#endif
        var rootDir = FileSystem.AppDataDirectory;
        BaseDir = SystemPath.Combine(rootDir, "MiniMikuDance", "data");
        WorkDir = BaseDir;
        Directory.CreateDirectory(BaseDir);
        Directory.CreateDirectory(SystemPath.Combine(BaseDir, "Movie"));
        LogService.WriteLine($"[MmdFileSystem] Using storage: {BaseDir}", LogService.LogLevel.Info);
    }

#if ANDROID
    public static void SetBaseUri(Android.Net.Uri uri)
    {
        BaseUri = uri;
    }
#endif

    public static string Ensure(string subdir)
    {
#if ANDROID
        if (BaseUri != null)
        {
            var context = Android.App.Application.Context;
            var doc = DocumentFile.FromTreeUri(context, BaseUri);
            if (doc != null)
            {
                foreach (var part in subdir.Split(SystemPath.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
                {
                    var next = doc.FindFile(part);
                    doc = next ?? doc.CreateDirectory(part);
                }
            }
        }
#endif
        var path = SystemPath.Combine(WorkDir, subdir);
        Directory.CreateDirectory(path);
        return path;
    }
}
