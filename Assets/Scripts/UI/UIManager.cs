using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private Button buttonPrefab;

    private UIConfig _config;

    /// <summary>
    /// Build the runtime UI from a JSON string.
    /// </summary>
    public void BuildUI(string configJson)
    {
        _config = JsonUtility.FromJson<UIConfig>(configJson);
        foreach (var btn in _config.buttons)
        {
            CreateButton(btn);
        }
    }

    /// <summary>
    /// Convenience method to load configuration from the StreamingAssets folder.
    /// </summary>
    public void BuildUIFromFile(string configPath)
    {
        var fullPath = Path.Combine(Application.streamingAssetsPath, configPath);
        var json = File.ReadAllText(fullPath);
        BuildUI(json);
    }

    /// <summary>
    /// Bind any additional callbacks after UI creation.
    /// Placeholder for future expansion.
    /// </summary>
    public void BindCallbacks()
    {
        // Example: connect buttons to other systems here
    }

    private void CreateButton(UIButtonConfig cfg)
    {
        var button = Instantiate(buttonPrefab, buttonContainer);
        var text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = cfg.label;
        }
        button.onClick.AddListener(() => OnButtonPressed(cfg.message));
    }

    private void OnButtonPressed(string message)
    {
        Debug.Log($"Button pressed: {message}");
    }
}
