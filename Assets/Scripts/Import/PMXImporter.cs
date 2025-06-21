using UnityEngine;

/// <summary>
/// Placeholder implementation for PMX model loading.
/// Real implementation should parse the PMX format and create a humanoid avatar.
/// </summary>
public static class PMXImporter
{
    public static GameObject Import(string path)
    {
        Debug.Log($"PMXImporter.Import: {path}");
#if UNITY_MMD_LOADER
        var bytes = System.IO.File.ReadAllBytes(path);
        var loader = new MMD.PMX.PMXLoader();
        var model = loader.Load(bytes);
        return model;
#else
        Debug.LogWarning("PMXImporter: MMD loader not available, returning dummy object");
        return new GameObject("PMXModel");
#endif
    }
}
