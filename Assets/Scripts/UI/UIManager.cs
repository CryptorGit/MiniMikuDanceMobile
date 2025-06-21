using System.IO;
using UnityEngine;
using UnityEngine.UI;

using System;

public class UIManager : MonoBehaviour
{
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Slider progressBarPrefab;
    [SerializeField] private Text messageTextPrefab;

    [SerializeField] private string configKey = "ui";

    private UIConfig _config;
    private Slider _progressBar;
    private Text _messageText;

    /// <summary>
    /// Fired when any runtime UI button is pressed.
    /// The string parameter contains the message defined in the config.
    /// </summary>
    public event Action<string> ButtonPressed;

    /// <summary>
    /// Build the runtime UI from a JSON string.
    /// </summary>
    public void BuildUI(string configJson)
    {
        var cfg = JsonUtility.FromJson<UIConfig>(configJson);
        BuildUI(cfg);
    }

    /// <summary>
    /// Build the runtime UI from a config object.
    /// </summary>
    public void BuildUI(UIConfig config)
    {
        _config = config;
        foreach (var btn in _config.buttons)
        {
            CreateButton(btn);
        }

        if (_config.showProgressBar && progressBarPrefab != null)
        {
            _progressBar = Instantiate(progressBarPrefab, buttonContainer);
            _progressBar.gameObject.SetActive(true);
            _progressBar.value = 0f;
        }

        if (_config.showMessage && messageTextPrefab != null)
        {
            _messageText = Instantiate(messageTextPrefab, buttonContainer);
            _messageText.gameObject.SetActive(true);
            _messageText.text = string.Empty;
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
    /// Load configuration via DataManager with a fallback to a bundled file.
    /// </summary>
    public void BuildUIFromDataManager(string defaultFile = "UIConfig.json")
    {
        UIConfig cfg = DataManager.LoadConfig<UIConfig>(configKey);
        if (cfg.buttons == null || cfg.buttons.Count == 0)
        {
            var path = Path.Combine(Application.streamingAssetsPath, defaultFile);
            var json = File.ReadAllText(path);
            cfg = JsonUtility.FromJson<UIConfig>(json);
            DataManager.SaveConfig(configKey, cfg);
        }
        BuildUI(cfg);
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
        ButtonPressed?.Invoke(message);
    }

    /// <summary>
    /// Update the progress bar value between 0 and 1.
    /// </summary>
    public void SetProgress(float value)
    {
        if (_progressBar != null)
        {
            _progressBar.value = Mathf.Clamp01(value);
        }
    }

    /// <summary>
    /// Display a status message in the UI.
    /// </summary>
    public void SetMessage(string message)
    {
        if (_messageText != null)
        {
            _messageText.text = message;
        }
    }
}
