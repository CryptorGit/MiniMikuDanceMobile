using UnityEngine;

/// <summary>
/// Bootstraps the application at startup.
/// Loads user settings and sets up core managers.
/// </summary>
public class AppInitializer : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private ModelImporter modelImporter;

    private AppSettings _settings;

    private void Awake()
    {
        // Load or create persistent settings
        _settings = AppSettings.Load();

        // Ensure references are assigned
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
        if (modelImporter == null)
        {
            modelImporter = FindObjectOfType<ModelImporter>();
        }

        // Build basic UI from default config packaged with the app
        uiManager?.BuildUIFromFile("UIConfig.json");
        uiManager?.BindCallbacks();

        Debug.Log($"AppInitializer: settings loaded from {AppSettings.FilePath}");
    }
}
