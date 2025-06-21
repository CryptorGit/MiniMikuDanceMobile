using System;
using UnityEngine;

/// <summary>
/// Application configuration container.
/// Stored as JSON in the persistent data path so settings survive app restarts.
/// </summary>
[Serializable]
public class AppSettings
{
    public int recordingWidth = 1280;
    public int recordingHeight = 720;
    public int recordingFPS = 30;
    public bool smoothing = true;

    private const string FileName = "settings.json";

    /// <summary>
    /// Full path to the settings file in persistentDataPath.
    /// </summary>
    public static string FilePath => System.IO.Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>
    /// Load settings from disk or return default values when the file is missing.
    /// </summary>
    public static AppSettings Load()
    {
        return JSONUtil.Load<AppSettings>(FilePath);
    }

    /// <summary>
    /// Save current settings to disk.
    /// </summary>
    public void Save()
    {
        JSONUtil.Save(FilePath, this);
    }
}
