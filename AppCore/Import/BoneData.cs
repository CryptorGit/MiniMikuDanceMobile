using System.Numerics;
using System.Collections.Generic;

namespace MiniMikuDance.Import;

public class BoneData
{
    public string Name { get; set; } = string.Empty;
    public int Parent { get; set; } = -1;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 Translation { get; set; } = Vector3.Zero;
    public Matrix4x4 BindMatrix { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 InverseBindMatrix { get; set; } = Matrix4x4.Identity;

    // IK 関連情報
    public bool IsIk { get; set; } = false;
    public int IkTargetIndex { get; set; } = -1;
    public List<int> IkChainIndices { get; set; } = new();
    public int IkLoopCount { get; set; } = 0;
    public float IkAngleLimit { get; set; } = 0f;

    // ツイスト補正量（0で無効）
    public float TwistWeight { get; set; } = 0f;
}
