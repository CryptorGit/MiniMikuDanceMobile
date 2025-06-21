using System;
using UnityEngine;

/// <summary>
/// User configurable application settings.
/// Stored as JSON via DataManager.
/// </summary>
[Serializable]
public class AppSettings
{
    public int videoWidth = 720;
    public int videoHeight = 1280;
    public int videoFps = 30;
    public float smoothingFactor = 0.5f;

    /// <summary>
    /// Load settings from persistent storage.
    /// </summary>
    public static AppSettings Load()
    {
        return DataManager.LoadConfig<AppSettings>("AppSettings");
    }

    /// <summary>
    /// Save these settings to persistent storage.
    /// </summary>
    public void Save()
    {
        DataManager.SaveConfig("AppSettings", this);
    }
}
