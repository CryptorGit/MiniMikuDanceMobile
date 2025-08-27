using System;
using System.IO;

namespace MiniMikuDance.Import;

public interface IMotionImporter : IDisposable
{
    MotionData ImportMotion(Stream stream);
}
