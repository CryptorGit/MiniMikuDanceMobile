using System.IO;
using UnityEngine;

/// <summary>
/// Very small helper that opens a share dialog for the given file path
/// on mobile platforms. Falls back to opening the file URL on other
/// targets.
/// </summary>
public static class ShareUtility
{
    public static void ShareFile(string path, string message = "")
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogWarning("ShareUtility.ShareFile: file not found");
            return;
        }

#if UNITY_ANDROID || UNITY_IOS
        Application.OpenURL($"file://{path}");
#else
        Application.OpenURL(path);
#endif
    }
}
