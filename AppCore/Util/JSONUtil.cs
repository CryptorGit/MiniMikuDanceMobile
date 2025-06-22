using System.Text.Json;

namespace MiniMikuDance.Util;

public static class JSONUtil
{
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
            var json = File.ReadAllText(path);
            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<T>(json, opts) ?? new T();
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
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }
}
