using System.IO;
using UnityEngine;

/// <summary>
/// Handles loading and saving of application data such as user settings
/// and recorded files. Uses JSONUtil for serialization.
/// </summary>
public static class DataManager
{
    private static string ConfigDir => Path.Combine(Application.persistentDataPath, "Config");
    private static string TempDir => Path.Combine(Application.persistentDataPath, "Temp");
    private static string RecordDir => Path.Combine(Application.persistentDataPath, "Recordings");

    public static T LoadConfig<T>(string key) where T : new()
    {
        var path = Path.Combine(ConfigDir, key + ".json");
        return JSONUtil.Load<T>(path);
    }

    public static void SaveConfig<T>(string key, T data)
    {
        var path = Path.Combine(ConfigDir, key + ".json");
        JSONUtil.Save(path, data);
    }

    /// <summary>
    /// Remove temporary files generated during processing.
    /// </summary>
    public static void CleanupTemp()
    {
        if (Directory.Exists(TempDir))
        {
            Directory.Delete(TempDir, true);
        }
    }

    /// <summary>
    /// Get a full path for saving a recording. Directories are created as needed.
    /// </summary>
    public static string GetRecordingPath(string filename)
    {
        var path = Path.Combine(RecordDir, filename);
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return path;
    }
}
