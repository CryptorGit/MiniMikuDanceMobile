using System.IO;
using UnityEngine;

public static class JSONUtil
{
    /// <summary>
    /// Load an object of type T from a JSON file.
    /// If the file does not exist, a new instance is returned.
    /// </summary>
    public static T Load<T>(string path) where T : new()
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"JSONUtil.Load: file not found at {path}");
            return new T();
        }

        var json = File.ReadAllText(path);
        return JsonUtility.FromJson<T>(json);
    }

    /// <summary>
    /// Save an object to a JSON file with pretty formatting.
    /// </summary>
    public static void Save<T>(string path, T data)
    {
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }
}
