using System.Numerics;
using System.Text.Json;
using MiniMikuDance.Util;
using Xunit;

public class Vector3JsonConverterTests
{
    [Fact]
    public void DeserializeVector3FromJson()
    {
        const string json = "{\"X\":1.0,\"Y\":-2.5,\"Z\":3.14}";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new Vector3JsonConverter());

        var result = JsonSerializer.Deserialize<Vector3>(json, options);
        Assert.Equal(new Vector3(1.0f, -2.5f, 3.14f), result);
    }
}
