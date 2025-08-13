using System;
using System.IO;
using MMDTools;

namespace MiniMikuDance.Import;

/// <summary>
/// PMX データをストリームから読み込む簡易リーダー。
/// 旧ライブラリのラッパーとして動作し、ソフトボディを安全にスキップします。
/// </summary>
public static class PmxStreamReader
{
    public static PMXObject Parse(Stream stream)
    {
        var pmx = PMXParser.Parse(stream);
        if (!pmx.SoftBodyList.IsEmpty)
        {
            Console.Error.WriteLine("ソフトボディは未対応のためスキップしました。");
        }
        return pmx;
    }
}
