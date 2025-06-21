using UnityEngine;

/// <summary>
/// Draws pose estimation joints and bones in the Scene view.
/// Useful for verifying EstimatorWorker output.
/// </summary>
public class PoseDebugVisualizer : MonoBehaviour
{
    public JointData[] frames;
    public Color jointColor = Color.cyan;
    public Color boneColor = Color.green;
    public float jointSize = 0.02f;
    public bool play = true;
    public bool loop = true;
    public float playbackSpeed = 1f;

    private int _index;
    private float _timer;
    private float _frameInterval = 1f / 30f;

    private static readonly int[,] Edges = new int[,]
    {
        {0,11},{0,12},{11,12},
        {11,13},{13,15},{12,14},{14,16},
        {23,24},{11,23},{12,24},
        {23,25},{25,27},{27,29},{29,31},
        {24,26},{26,28},{28,30},{30,32}
    };

    /// <summary>
    /// Assign pose data to visualize.
    /// </summary>
    public void SetFrames(JointData[] data)
    {
        frames = data;
        _index = 0;
        _timer = 0f;
        if (frames != null && frames.Length > 1)
            _frameInterval = frames[1].timestamp - frames[0].timestamp;
    }

    private void Update()
    {
        if (!play || frames == null || frames.Length == 0)
            return;

        _timer += Time.deltaTime * playbackSpeed;
        if (_timer >= _frameInterval)
        {
            _timer -= _frameInterval;
            _index++;
            if (_index >= frames.Length)
            {
                if (loop) _index = 0;
                else
                {
                    _index = frames.Length - 1;
                    play = false;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (frames == null || frames.Length == 0)
            return;

        var joints = frames[_index].positions;
        if (joints == null)
            return;

        Gizmos.color = jointColor;
        foreach (var pos in joints)
        {
            Gizmos.DrawSphere(pos, jointSize);
        }

        Gizmos.color = boneColor;
        int count = Edges.GetLength(0);
        for (int i = 0; i < count; i++)
        {
            int a = Edges[i,0];
            int b = Edges[i,1];
            if (a < joints.Length && b < joints.Length)
            {
                Gizmos.DrawLine(joints[a], joints[b]);
            }
        }
    }
}
