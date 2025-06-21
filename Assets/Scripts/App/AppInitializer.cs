using UnityEngine;
using System.IO;

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
        if (uiManager != null)
        {
            uiManager.BindCallbacks();
            uiManager.ButtonPressed += HandleUIButton;
        }

        Debug.Log($"AppInitializer: settings loaded from {AppSettings.FilePath}");
    }

    private void HandleUIButton(string message)
    {
        switch (message)
        {
            case "load_model":
                var modelPath = Path.Combine(Application.streamingAssetsPath, "SampleModel.vrm");
                modelImporter?.ImportModel(modelPath);
                break;
        }
    }
}
