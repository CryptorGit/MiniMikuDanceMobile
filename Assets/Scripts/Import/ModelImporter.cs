using System.IO;
using UnityEngine;

/// <summary>
/// Utility class for loading external 3D models at runtime.
/// Currently acts as a thin wrapper around specialized importers and
/// provides simple avatar retrieval from loaded models.
/// </summary>
public class ModelImporter : MonoBehaviour
{
    /// <summary>
    /// Import a model file and instantiate it in the scene.
    /// Only VRM, FBX and PMX files are recognised. Unsupported
    /// extensions will simply log a warning.
    /// </summary>
    public GameObject ImportModel(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("ModelImporter.ImportModel: path is null or empty");
            return null;
        }

        var ext = Path.GetExtension(path).ToLowerInvariant();
        GameObject model = null;

        switch (ext)
        {
            case ".vrm":
                model = VRMImporter.Import(path);
                break;
            case ".pmx":
                model = PMXImporter.Import(path);
                break;
            case ".fbx":
                model = LoadFbx(path);
                break;
            default:
                Debug.LogWarning($"ModelImporter: unsupported extension {ext}");
                break;
        }

        return model;
    }

    /// <summary>
    /// Attempt to retrieve a Humanoid avatar from the loaded model.
    /// </summary>
    public Avatar CreateAvatar(GameObject model)
    {
        var animator = model?.GetComponent<Animator>();
        if (animator != null)
        {
            return animator.avatar;
        }

        Debug.LogWarning("ModelImporter.CreateAvatar: Animator not found");
        return null;
    }

    private GameObject LoadFbx(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var prefab = Resources.Load<GameObject>(name);
        return prefab != null ? Instantiate(prefab) : null;
    }
}
