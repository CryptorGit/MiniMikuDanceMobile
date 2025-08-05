using System.Collections.Generic;
using System.Numerics;

namespace MiniMikuDance.Motion;

public class PoseSnapshot
{
    public List<Vector3> IkTargets { get; set; } = new();
    public List<bool> IkEnabled { get; set; } = new();
    public List<int> IkGoalIndices { get; set; } = new();
    public List<Vector3> BoneRotations { get; set; } = new();
    public List<Vector3> BoneTranslations { get; set; } = new();
}

