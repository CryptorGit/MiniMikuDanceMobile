using UnityEngine;

/// <summary>
/// Logs average frame rate and memory usage at a fixed interval.
/// Intended for lightweight on-device performance monitoring.
/// </summary>
public class PerformanceLogger : MonoBehaviour
{
    [SerializeField]
    private float logInterval = 5f;

    private int _frameCount;
    private float _timer;

    private void Update()
    {
        _frameCount++;
        _timer += Time.unscaledDeltaTime;
        if (_timer >= logInterval)
        {
            float fps = _frameCount / _timer;
            long mem = System.GC.GetTotalMemory(false) / (1024 * 1024);
            Debug.Log($"PerformanceLogger: {fps:F1} fps, {mem} MB memory used");
            _frameCount = 0;
            _timer = 0f;
        }
    }
}
