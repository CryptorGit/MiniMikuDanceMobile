using System.Numerics;
using System.Text.Json;
using MiniMikuDance.Util;
using Xunit;

public class Vector3JsonConverterTests
{
    [Fact]
    public void SerializeDeserialize_Vector3()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new Vector3JsonConverter());
        var vec = new Vector3(1.1f, 2.2f, 3.3f);
        var json = JsonSerializer.Serialize(vec, options);
        Assert.Contains("\"X\"", json);
        var des = JsonSerializer.Deserialize<Vector3>(json, options);
        Assert.Equal(vec, des);
    }
}
