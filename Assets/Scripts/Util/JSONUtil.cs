using System.IO;
using UnityEngine;

public static class JSONUtil
{
    /// <summary>
    /// Load an object of type T from a JSON file. If missing, a placeholder file is created.
    /// </summary>
    public static T Load<T>(string path) where T : new()
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"JSONUtil.Load: file not found at {path}. Creating default.");
            var placeholder = new T();
            Save(path, placeholder);
            return placeholder;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"JSONUtil.Load: failed to read {path}. {ex}");
            return new T();
        }
    }

    /// <summary>
    /// Save an object to a JSON file with pretty formatting. Creates directories when needed.
    /// </summary>
    public static void Save<T>(string path, T data)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"JSONUtil.Save: failed to write {path}. {ex}");
        }
    }
}
