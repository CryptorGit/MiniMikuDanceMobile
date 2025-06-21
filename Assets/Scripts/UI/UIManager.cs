using System.IO;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections.Generic;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Toggle togglePrefab;
    [SerializeField] private Slider progressBarPrefab;
    [SerializeField] private Text messageTextPrefab;
    [SerializeField] private Text errorTextPrefab;
    [SerializeField] private Image recordingIndicatorPrefab;
    [SerializeField] private RawImage thumbnailPrefab;

    [SerializeField] private string configKey = "ui";

    private UIConfig _config;
    private Slider _progressBar;
    private Text _messageText;
    private Text _errorText;
    private Coroutine _hideErrorRoutine;
    private Image _recordingIndicator;
    private RawImage _thumbnail;
    private readonly Dictionary<string, Toggle> _toggles = new Dictionary<string, Toggle>();

    /// <summary>
    /// Fired when a UI toggle value changes.
    /// The string parameter is the toggle id, bool is the new state.
    /// </summary>
    public event Action<string, bool> ToggleChanged;

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

        foreach (var tgl in _config.toggles)
        {
            CreateToggle(tgl);
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

        if (_config.showMessage)
        {
            if (errorTextPrefab != null)
            {
                _errorText = Instantiate(errorTextPrefab, buttonContainer);
            }
            else if (messageTextPrefab != null)
            {
                _errorText = Instantiate(messageTextPrefab, buttonContainer);
            }
            if (_errorText != null)
            {
                _errorText.gameObject.SetActive(false);
                _errorText.color = Color.red;
            }
        }

        if (_config.showRecordingIndicator)
        {
            if (recordingIndicatorPrefab != null)
            {
                _recordingIndicator = Instantiate(recordingIndicatorPrefab, buttonContainer);
            }
            else
            {
                var go = new GameObject("RecordingIndicator", typeof(Image));
                go.transform.SetParent(buttonContainer, false);
                var img = go.GetComponent<Image>();
                img.color = Color.red;
                img.rectTransform.sizeDelta = new Vector2(20f, 20f);
                _recordingIndicator = img;
            }
            _recordingIndicator.gameObject.SetActive(false);
        }

        if (_config.showThumbnail)
        {
            if (thumbnailPrefab != null)
            {
                _thumbnail = Instantiate(thumbnailPrefab, buttonContainer);
            }
            else
            {
                var go = new GameObject("Thumbnail", typeof(RawImage));
                go.transform.SetParent(buttonContainer, false);
                _thumbnail = go.GetComponent<RawImage>();
                _thumbnail.rectTransform.sizeDelta = new Vector2(100f, 100f);
            }
            _thumbnail.gameObject.SetActive(false);
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

    private void CreateToggle(UIToggleConfig cfg)
    {
        if (togglePrefab == null)
            return;

        var toggle = Instantiate(togglePrefab, buttonContainer);
        var text = toggle.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = cfg.label;
        }
        toggle.isOn = cfg.defaultValue;
        toggle.onValueChanged.AddListener(value => OnToggleChanged(cfg.id, value));
        _toggles[cfg.id] = toggle;
    }

    private void OnButtonPressed(string message)
    {
        Debug.Log($"Button pressed: {message}");
        ButtonPressed?.Invoke(message);
    }

    private void OnToggleChanged(string id, bool value)
    {
        Debug.Log($"Toggle changed: {id}={value}");
        ToggleChanged?.Invoke(id, value);
    }

    /// <summary>
    /// Programmatically set the value of a toggle by id.
    /// </summary>
    public void SetToggle(string id, bool value)
    {
        if (_toggles.TryGetValue(id, out var toggle))
        {
            toggle.isOn = value;
        }
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

    /// <summary>
    /// Display an error message in the UI for a short time.
    /// </summary>
    public void ShowError(string message, float duration = 2f)
    {
        if (_errorText == null)
            return;

        _errorText.text = message;
        _errorText.gameObject.SetActive(true);
        if (_hideErrorRoutine != null)
        {
            StopCoroutine(_hideErrorRoutine);
        }
        _hideErrorRoutine = StartCoroutine(HideErrorAfter(duration));
    }

    private IEnumerator HideErrorAfter(float time)
    {
        yield return new WaitForSeconds(time);
        if (_errorText != null)
        {
            _errorText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Show or hide the recording indicator.
    /// </summary>
    public void SetRecordingIndicator(bool recording)
    {
        if (_recordingIndicator != null)
        {
            _recordingIndicator.gameObject.SetActive(recording);
        }
    }

    /// <summary>
    /// Display a thumbnail image loaded from the given file path.
    /// Pass null or invalid path to hide the thumbnail.
    /// </summary>
    public void SetThumbnail(string path)
    {
        if (_thumbnail == null)
            return;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            _thumbnail.gameObject.SetActive(false);
            return;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            _thumbnail.texture = tex;
            _thumbnail.gameObject.SetActive(true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UIManager.SetThumbnail: {ex}");
            _thumbnail.gameObject.SetActive(false);
        }
    }
}
