using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_SENTIS
using Unity.Sentis;
#endif

/// <summary>
/// Handles low level Sentis inference for pose estimation models.
/// </summary>
public class EstimatorWorker : IDisposable
{
#if UNITY_SENTIS
    private Model _model;
    private IWorker _worker;
#endif

    /// <summary>
    /// Load an ONNX model from the StreamingAssets folder.
    /// </summary>
    public void LoadModel(string modelFile)
    {
#if UNITY_SENTIS
        var path = Path.Combine(Application.streamingAssetsPath, modelFile);
        var bytes = File.ReadAllBytes(path);
        _model = ModelLoader.Load(bytes);
        _worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, _model);
#else
        Debug.LogWarning("EstimatorWorker.LoadModel: Sentis is not available.");
#endif
    }

    /// <summary>
    /// Run inference on the given texture and return raw float outputs.
    /// </summary>
    public float[] Run(Texture2D input)
    {
#if UNITY_SENTIS
        using var tensor = TextureConverter.ToTensor(input, 3);
        _worker.Execute(tensor);
        using var output = _worker.PeekOutput() as TensorFloat;
        return output.ToReadOnlyArray();
#else
        Debug.LogWarning("EstimatorWorker.Run called without Sentis support.");
        return Array.Empty<float>();
#endif
    }

    public void Dispose()
    {
#if UNITY_SENTIS
        _worker?.Dispose();
#endif
    }
}
