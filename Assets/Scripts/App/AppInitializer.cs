using UnityEngine;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Bootstraps the application at startup.
/// Loads user settings and sets up core managers.
/// </summary>
public class AppInitializer : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private ModelImporter modelImporter;
    [SerializeField] private PoseEstimator poseEstimator;
    [SerializeField] private MotionGenerator motionGenerator;
    [SerializeField] private MotionPlayer motionPlayer;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private RecorderController recorderController;

    private AppSettings _settings;
    private JointData[] _lastJoints;
    private MotionData _motion;
    private bool _isRecording;

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
        if (poseEstimator == null)
        {
            poseEstimator = FindObjectOfType<PoseEstimator>();
        }
        if (motionGenerator == null)
        {
            motionGenerator = FindObjectOfType<MotionGenerator>();
        }
        if (motionPlayer == null)
        {
            motionPlayer = FindObjectOfType<MotionPlayer>();
        }
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }
        if (recorderController == null)
        {
            recorderController = FindObjectOfType<RecorderController>();
        }

        // Build basic UI from default config packaged with the app
        uiManager?.BuildUIFromFile("UIConfig.json");
        if (uiManager != null)
        {
            uiManager.BindCallbacks();
            uiManager.ButtonPressed += HandleUIButton;
        }

        cameraController?.EnableGyro(true);

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
            case "analyze_video":
                var videoPath = Path.Combine(Application.streamingAssetsPath, "SampleDance.mp4");
                _ = RunPoseEstimation(videoPath);
                break;
            case "generate_motion":
                if (_lastJoints != null && motionGenerator != null)
                {
                    _motion = motionGenerator.GenerateData(_lastJoints);
                    motionGenerator.Smooth(_motion, 2);
                    Debug.Log($"MotionGenerator produced {_motion.boneCurves.Count} curves");
                }
                break;
            case "play_motion":
                if (_motion != null && motionPlayer != null)
                {
                    motionPlayer.LoadMotion(_motion);
                    motionPlayer.Play();
                }
                break;
            case "toggle_camera":
                if (cameraController != null)
                {
                    var enable = !Input.gyro.enabled;
                    cameraController.EnableGyro(enable);
                    Debug.Log($"Gyro mode {(enable ? "on" : "off")}");
                }
                break;
            case "toggle_record":
                if (recorderController != null)
                {
                    if (!recorderController.enabled)
                    {
                        // ensure component enabled before recording
                        recorderController.enabled = true;
                    }

                    if (IsRecording())
                    {
                        recorderController.StopRecording();
                        Debug.Log($"Recording saved to {recorderController.GetSavedPath()}");
                        _isRecording = false;
                    }
                    else
                    {
                        recorderController.StartRecording(1280, 720, 30);
                        _isRecording = true;
                    }
                }
                break;
        }
    }

    private async Task RunPoseEstimation(string path)
    {
        if (poseEstimator == null)
        {
            Debug.LogWarning("PoseEstimator not assigned");
            return;
        }

        _lastJoints = await poseEstimator.EstimateMotion(path);
        Debug.Log($"PoseEstimator returned {_lastJoints.Length} frames");
    }

    private bool IsRecording() => _isRecording;
}
