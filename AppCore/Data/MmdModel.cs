using System.Collections.Generic;
using MiniMikuDance.IK;

namespace MiniMikuDance.Data;

public class MmdModel
{
    public List<IkChain> IkChains { get; } = new();
    public List<IkChain> FootIkChains { get; } = new();
    public IkBone? RootBone { get; set; }

    // 物理用の剛体・ジョイント定義は後で追加する
}
