using System.IO;
using UnityEngine;
#if UNITY_BARRACUDA
using Unity.Barracuda;
#endif

/// <summary>
/// Handles loading and executing the pose estimation ONNX model.
/// This is a simplified placeholder for Unity Sentis integration.
/// </summary>
public class EstimatorWorker
{
    private const string ModelFile = "pose_model.onnx";
    private bool _loaded;
    private byte[] _modelData;
#if UNITY_BARRACUDA
    private Model _model;
    private IWorker _worker;
#endif

    /// <summary>
    /// Load the ONNX model from StreamingAssets if not already loaded.
    /// </summary>
    public void LoadModel()
    {
        if (_loaded) return;

        var path = Path.Combine(Application.streamingAssetsPath, ModelFile);
        if (!File.Exists(path))
        {
            Debug.LogError($"EstimatorWorker: model not found at {path}");
            return;
        }

        // Load the ONNX bytes so a Barracuda or Sentis model can be created
        _modelData = File.ReadAllBytes(path);
#if UNITY_BARRACUDA
        try
        {
            _model = ModelLoader.Load(_modelData);
            _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, _model);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EstimatorWorker: failed to create worker. {ex}");
        }
#elif UNITY_SENTIS
        // Sentis integration would load the model here
#endif
        Debug.Log($"EstimatorWorker: loaded model from {path}");
        _loaded = true;
    }

    /// <summary>
    /// Run inference on the given texture and return normalized joint vectors.
    /// The returned array contains 33 landmark positions in local space.
    /// </summary>
    public Vector3[] Run(Texture2D frame)
    {
        if (!_loaded)
        {
            LoadModel();
        }

        if (frame == null)
        {
            Debug.LogWarning("EstimatorWorker.Run: frame is null");
            return new Vector3[0];
        }

#if UNITY_BARRACUDA
        if (_worker != null)
        {
            using var input = new Tensor(frame, 3);
            _worker.Execute(input);
            using var output = _worker.PeekOutput();

            int jointCount = Mathf.Min(33, output.shape.length / 3);
            var joints = new Vector3[jointCount];
            for (int i = 0; i < jointCount; i++)
            {
                float x = output[0, i * 3];
                float y = output[0, i * 3 + 1];
                float z = output[0, i * 3 + 2];
                joints[i] = new Vector3(x, y, z);
            }
            input.Dispose();
            return joints;
        }
#endif

        // Placeholder inference - return random positions
        var randomJoints = new Vector3[33];
        for (int i = 0; i < randomJoints.Length; i++)
        {
            randomJoints[i] = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f));
        }

        // Real implementation would feed the texture to Sentis and read outputs
        return randomJoints;
    }

#if UNITY_BARRACUDA
    /// <summary>
    /// Dispose the underlying Barracuda worker when no longer needed.
    /// </summary>
    public void Dispose()
    {
        _worker?.Dispose();
        _worker = null;
        _model = null;
    }
#endif
}
