using System;
using OpenTK.Mathematics;

namespace ViewerApp;

public class Viewer
{
    public Vector2i Size { get; private set; } = new Vector2i(640, 480);

    public event Action<float>? FrameUpdated;

    public Viewer(string modelPath)
    {
        // モデル読み込み処理は未実装
    }

    public void SetViewMatrix(Matrix4 view)
    {
        // ビュー行列適用処理は未実装
    }

    public byte[] CaptureFrame()
    {
        // フレームをキャプチャして返す処理は未実装
        return Array.Empty<byte>();
    }

    // 簡易的なフレーム更新用メソッド
    public void Update(float deltaTime)
    {
        FrameUpdated?.Invoke(deltaTime);
    }
}
