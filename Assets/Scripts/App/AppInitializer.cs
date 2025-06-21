using UnityEngine;

/// <summary>
/// Bootstraps the application at runtime.
/// Loads user settings and prepares core components.
/// </summary>
public class AppInitializer : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PoseEstimator poseEstimator;

    private AppSettings _settings;

    private void Start()
    {
        _settings = AppSettings.Load();
        uiManager.BuildUIFromFile("UIConfig.json");
        uiManager.BindCallbacks();
    }

    private void OnApplicationQuit()
    {
        _settings.Save();
    }
}
