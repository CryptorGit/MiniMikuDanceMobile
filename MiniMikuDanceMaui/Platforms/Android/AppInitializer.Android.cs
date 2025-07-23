#if ANDROID
using MiniMikuDance.App;
using MiniMikuDance.PoseEstimation;

namespace MiniMikuDance.App;

public partial class AppInitializer
{
    partial void ConfigureFrameExtractor(ref IVideoFrameExtractor extractor)
    {
        extractor = new AndroidFrameExtractor();
    }
}
#endif
