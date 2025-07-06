namespace MiniMikuDance.PoseEstimation;

public class JointData
{
    public float Timestamp { get; set; }
    public System.Numerics.Vector3[] Positions { get; set; } = System.Array.Empty<System.Numerics.Vector3>();
    public float[] Confidences { get; set; } = System.Array.Empty<float>();
}
