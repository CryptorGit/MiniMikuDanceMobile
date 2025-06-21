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
#if UNITY_VRM
        // When UniVRM is available, load the model from the specified file
        var bytes = System.IO.File.ReadAllBytes(path);
        var context = new VRM.VrmLoader(bytes).Load();
        return context.Root;
#else
        Debug.LogWarning("VRMImporter: UniVRM not available, returning dummy object");
        return new GameObject("VRMModel");
#endif
    }
}
