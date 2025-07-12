using System;


namespace MiniMikuDance.PoseEstimation;

/// <summary>
/// JointData 配列を簡易的に可視化するデバッグ用クラス。
/// 各フレームの座標をコンソールに出力する。
/// </summary>
public class PoseDebugVisualizer
{
    private JointData[] _frames = Array.Empty<JointData>();
    private int _index;

    /// <summary>
    /// 表示する JointData 配列を設定する。
    /// </summary>
    public void SetFrames(JointData[] data)
    {
        _frames = data ?? Array.Empty<JointData>();
        _index = 0;
    }

    /// <summary>
    /// 次のフレームの JointData をコンソールに出力する。
    /// </summary>
    public void PrintNextFrame()
    {
        if (_frames.Length == 0 || _index >= _frames.Length)
            return;

        var frame = _frames[_index];

        for (int i = 0; i < frame.Positions.Length; i++)
        {
            var p = frame.Positions[i];
            float c = i < frame.Confidences.Length ? frame.Confidences[i] : 0f;

        }
        _index++;
    }
}
