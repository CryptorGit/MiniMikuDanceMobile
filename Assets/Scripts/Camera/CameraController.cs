using UnityEngine;

/// <summary>
/// Synchronizes the attached camera with device sensors.
/// Supports gyroscope based rotation and simple mouse look in the editor.
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField] private bool useGyro = true;
    [SerializeField] private float mouseSensitivity = 1f;
    [SerializeField] private ARPoseManager arPoseManager;

    private bool _gyroEnabled;
    private Quaternion _baseRotation;

    private void Start()
    {
        _baseRotation = transform.localRotation;
        EnableGyro(useGyro);
    }

    /// <summary>
    /// Enable or disable gyroscope driven camera.
    /// </summary>
    public void EnableGyro(bool on)
    {
        _gyroEnabled = on && SystemInfo.supportsGyroscope;
        Input.gyro.enabled = _gyroEnabled;
    }

    private void Update()
    {
#if UNITY_ARFOUNDATION
        if (arPoseManager != null)
        {
            SyncARPose();
        }
#endif

        if (_gyroEnabled)
        {
            SyncGyro();
        }
        else
        {
#if UNITY_EDITOR
            HandleMouseLook();
#endif
        }
    }

    /// <summary>
    /// Apply device gyroscope attitude to the camera transform.
    /// </summary>
    public void SyncGyro()
    {
        var q = Input.gyro.attitude;
        q = new Quaternion(q.x, q.y, -q.z, -q.w); // convert to Unity coordinates
        transform.localRotation = _baseRotation * q;
    }

    /// <summary>
    /// Apply ARFoundation pose to the camera transform if available.
    /// </summary>
    public void SyncARPose()
    {
#if UNITY_ARFOUNDATION
        if (arPoseManager != null)
        {
            var pose = arPoseManager.CurrentPose;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
#endif
    }

#if UNITY_EDITOR
    private void HandleMouseLook()
    {
        float x = Input.GetAxis("Mouse X") * mouseSensitivity;
        float y = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(Vector3.up, x, Space.World);
        transform.Rotate(Vector3.right, -y, Space.Self);
    }
#endif
}
