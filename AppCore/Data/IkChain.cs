using System.Collections.Generic;
using MiniMikuDance.Import;

namespace MiniMikuDance.Data;

public class IkChain
{
    public int Target { get; set; } = -1;
    public List<IkLink> Links { get; } = new();
    public int Iterations { get; set; }
        = 0;
    public float ControlWeight { get; set; }
        = 0f;
}

public class FootIkChain : IkChain
{
    public int Ankle { get; set; } = -1;
}

