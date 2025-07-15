namespace MiniMikuDance.Import;

public class JointLimit
{
    public Range X { get; set; } = new();
    public Range Y { get; set; } = new();
    public Range Z { get; set; } = new();
}

public class Range
{
    public float Min { get; set; }
    public float Max { get; set; }
}

public class JointLimitConfig
{
    public Dictionary<string, JointLimit> Limits { get; set; } = new();
}
