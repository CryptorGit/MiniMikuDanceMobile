namespace MiniMikuDance.Util;

public abstract class Singleton<T> where T : class, new()
{
    public static T Instance { get; } = new();
}
