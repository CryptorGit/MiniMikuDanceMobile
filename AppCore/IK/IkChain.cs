using System.Collections.Generic;

namespace MiniMikuDance.IK;

public class IkChain
{
    /// <summary>
    /// 末端ボーン名
    /// </summary>
    public string EndBoneName { get; set; } = string.Empty;

    /// <summary>
    /// ルートから末端までのボーンインデックス
    /// </summary>
    public List<int> Indices { get; } = new();
}
