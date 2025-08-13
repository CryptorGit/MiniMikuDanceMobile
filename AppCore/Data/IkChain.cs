using System.Collections.Generic;

namespace MiniMikuDance.Data;

public class IkChain
{
    public int Target { get; set; } = -1;
    public List<int> Links { get; } = new();
    public int Iterations { get; set; }
        = 0;
    public float ControlWeight { get; set; }
        = 0f;
}

public class FootIkChain : IkChain
{
    public int Ankle { get; set; } = -1;
}

