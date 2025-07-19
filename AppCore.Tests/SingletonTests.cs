using MiniMikuDance.Util;
using Xunit;

public class SingletonTests
{
    private class DummySingleton : Singleton<DummySingleton>
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Instance_ReturnsSameObject()
    {
        var a = DummySingleton.Instance;
        var b = DummySingleton.Instance;
        a.Value = 42;
        Assert.Same(a, b);
        Assert.Equal(42, b.Value);
    }
}
