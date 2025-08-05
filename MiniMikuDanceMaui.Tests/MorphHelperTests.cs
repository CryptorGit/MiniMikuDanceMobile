using MiniMikuDance.Import;
using MiniMikuDanceMaui;
using Xunit;

namespace MiniMikuDanceMaui.Tests;

public class MorphHelperTests
{
    [Fact]
    public void BuildMorphEntries_DuplicateNamesAreIndexed()
    {
        var model = new ModelData
        {
            Morphs = new List<MorphData>
            {
                new() { Name = "smile" },
                new() { Name = "Smile" },
                new() { Name = "smile " }
            }
        };

        var entries = MorphHelper.BuildMorphEntries(model).ToList();

        Assert.Equal(3, entries.Count);
        Assert.Equal(("smile", "smile (1)"), entries[0]);
        Assert.Equal(("Smile", "Smile (2)"), entries[1]);
        Assert.Equal(("smile ", "smile (3)"), entries[2]);
    }
}
