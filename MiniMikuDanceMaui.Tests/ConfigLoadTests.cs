using Xunit;
using MiniMikuDance.Data;
using MiniMikuDance.App;

public class ConfigLoadTests
{
    [Fact]
    public void BonesConfig_Loaded_FromOutput()
    {
        var cfg = DataManager.Instance.LoadConfig<BonesConfig>("BonesConfig");
        Assert.NotNull(cfg);
        Assert.NotEmpty(cfg.HumanoidBoneLimits);
    }
}
