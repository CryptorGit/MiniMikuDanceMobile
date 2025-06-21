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
        // TODO: integrate MMD loader
        return new GameObject("PMXModel");
    }
}
