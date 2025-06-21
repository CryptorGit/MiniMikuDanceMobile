using System.IO;
using UnityEngine;

/// <summary>
/// Centralized helper for loading and saving persistent configuration files
/// and managing temporary data directories.
/// </summary>
public static class DataManager
{
    private static string ConfigRoot => Path.Combine(Application.persistentDataPath, "config");
    private static string TempRoot => Application.temporaryCachePath;

    /// <summary>
    /// Load an object of type T from persistent storage.
    /// If the file does not exist a new object is created and saved.
    /// </summary>
    public static T LoadConfig<T>(string key) where T : new()
    {
        var path = Path.Combine(ConfigRoot, key + ".json");
        return JSONUtil.Load<T>(path);
    }

    /// <summary>
    /// Save an object to persistent storage.
    /// </summary>
    public static void SaveConfig<T>(string key, T data)
    {
        var path = Path.Combine(ConfigRoot, key + ".json");
        JSONUtil.Save(path, data);
    }

    /// <summary>
    /// Delete all files in the temporary cache directory.
    /// </summary>
    public static void CleanupTemp()
    {
        if (!Directory.Exists(TempRoot))
            return;

        try
        {
            Directory.Delete(TempRoot, true);
            Directory.CreateDirectory(TempRoot);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DataManager.CleanupTemp: {ex}");
        }
    }
}
