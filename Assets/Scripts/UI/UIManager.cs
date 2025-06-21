using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private Button buttonPrefab;

    private UIConfig _config;

    /// <summary>
    /// Build the runtime UI from a JSON configuration file.
    /// </summary>
    public void BuildUI(string configPath)
    {
        var fullPath = Path.Combine(Application.streamingAssetsPath, configPath);
        _config = JSONUtil.Load<UIConfig>(fullPath);
        foreach (var btn in _config.buttons)
        {
            CreateButton(btn);
        }
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
