using UnityEngine;
#if UNITY_ARFOUNDATION
using UnityEngine.XR.ARFoundation;
#endif

/// <summary>
/// Provides the current AR camera pose when ARFoundation is available.
/// This pose can be consumed by CameraController for AR tracking.
/// </summary>
public class ARPoseManager : MonoBehaviour
{
#if UNITY_ARFOUNDATION
    private ARSession _session;
    private UnityEngine.XR.ARFoundation.ARCameraManager _cameraManager;
    public Pose CurrentPose { get; private set; }

    private void Awake()
    {
        _session = FindObjectOfType<ARSession>();
        _cameraManager = GetComponent<UnityEngine.XR.ARFoundation.ARCameraManager>();
    }

    private void OnEnable()
    {
        if (_cameraManager != null)
            _cameraManager.frameReceived += OnFrameReceived;
    }

    private void OnDisable()
    {
        if (_cameraManager != null)
            _cameraManager.frameReceived -= OnFrameReceived;
    }

    private void OnFrameReceived(ARCameraFrameEventArgs args)
    {
        var cam = _cameraManager.GetComponent<Camera>();
        if (cam != null)
        {
            CurrentPose = new Pose(cam.transform.position, cam.transform.rotation);
        }
    }
#else
    public Pose CurrentPose => new Pose(transform.position, transform.rotation);
#endif
}
