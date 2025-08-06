using System.Text.Json;

namespace MiniMikuDance.Util;

public static class JSONUtil
{
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

    public static T Load<T>(string path) where T : new()
    {
        try
        {
            if (!File.Exists(path))
            {
                var placeholder = new T();
                Save(path, placeholder);
                return placeholder;
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, CreateOptions()) ?? new T();
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
        var json = JsonSerializer.Serialize(data, CreateOptions(true));
        File.WriteAllText(path, json);
    }
}
