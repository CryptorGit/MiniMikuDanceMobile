using UnityEngine;

/// <summary>
/// Minimal placeholder for runtime VRM loading.
/// Replace with UniVRM integration in the future.
/// </summary>
public static class VRMImporter
{
    public static GameObject Import(string path)
    {
        Debug.Log($"VRMImporter.Import: {path}");
        // TODO: implement using UniVRM
        return new GameObject("VRMModel");
    }
}
