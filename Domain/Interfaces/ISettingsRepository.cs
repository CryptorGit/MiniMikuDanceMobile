namespace MiniMikuDance.Domain.Interfaces;

/// <summary>
/// 設定の保存や一時ディレクトリ管理を抽象化したリポジトリ。
/// </summary>
public interface ISettingsRepository
{
    T LoadConfig<T>(string key) where T : new();
    void SaveConfig<T>(string key, T data);
    string TempDir { get; }
    void CleanupTemp();
}
