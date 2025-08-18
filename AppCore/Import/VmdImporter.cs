using System.IO;

namespace MiniMikuDance.Import;

public class VmdImporter : IMotionImporter
{
    public MotionData ImportMotion(Stream stream)
    {
        // TODO: PMXParser を用いた VMD の読み込みを実装する
        throw new System.NotImplementedException();
    }

    public void Dispose()
    {
    }
}
