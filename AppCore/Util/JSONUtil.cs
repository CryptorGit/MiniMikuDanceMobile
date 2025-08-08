using System.Text.Json;

namespace MiniMikuDance.Util;

public static class JSONUtil
{
    private static readonly JsonSerializerOptions Options = CreateOptions();
    private static readonly JsonSerializerOptions IndentedOptions = CreateOptions(writeIndented: true);

    private static JsonSerializerOptions CreateOptions(bool writeIndented = false)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = writeIndented
        };
        opts.Converters.Add(new Vector3JsonConverter());
        return opts;
    }

    public static T LoadFromStream<T>(Stream stream) where T : new()
    {
        try
        {
            var result = JsonSerializer.DeserializeAsync<T>(stream, Options).GetAwaiter().GetResult();
            return result ?? new T();
        }
        catch
        {
            return new T();
        }
    }

    public static T Load<T>(string path) where T : new()
    {
        if (!File.Exists(path))
        {
            var placeholder = new T();
            Save(path, placeholder);
            return placeholder;
        }

        try
        {
            using var fs = File.OpenRead(path);
            var result = JsonSerializer.DeserializeAsync<T>(fs, Options).GetAwaiter().GetResult();
            return result ?? new T();
        }
        catch
        {
            return new T();
        }
    }

    public static void Save<T>(string path, T data)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir!);
        var json = JsonSerializer.Serialize(data, IndentedOptions);
        File.WriteAllText(path, json);
    }
}
