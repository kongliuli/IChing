namespace IChing.Client.Shared.Settings;

public sealed record ProviderPreset(string Id, string DisplayName, string BaseUrl, string Model);

/// <summary>
/// 自助版提供商预设表。
/// </summary>
public static class ProviderPresets
{
    public static ProviderPreset DeepSeek { get; } = new(
        "deepseek", "DeepSeek", "https://api.deepseek.com/v1", "deepseek-chat");

    public static ProviderPreset OpenAi { get; } = new(
        "openai", "OpenAI", "https://api.openai.com/v1", "gpt-4o-mini");

    public static ProviderPreset Zhipu { get; } = new(
        "zhipu", "智谱", "https://open.bigmodel.cn/api/paas/v4", "glm-4-flash");

    public static ProviderPreset Ollama { get; } = new(
        "ollama", "Ollama (本地)", "http://localhost:11434/v1", "qwen3.5:9b");

    public static IReadOnlyList<ProviderPreset> All { get; } =
        [DeepSeek, OpenAi, Zhipu, Ollama];

    public static ProviderPreset? Find(string id) =>
        All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static void Apply(IClientRuntimeSettings settings, string providerId)
    {
        var preset = Find(providerId) ?? DeepSeek;
        if (settings is MutableClientRuntimeSettings mutable)
        {
            mutable.Provider = preset.Id;
            mutable.BaseUrl = preset.BaseUrl;
            mutable.Model = preset.Model;
            return;
        }

        // Preferences-backed settings implement apply themselves; this path is for Mutable only.
        throw new InvalidOperationException(
            "Apply ProviderPresets via MutableClientRuntimeSettings or AppSettings.ApplyProviderPreset.");
    }
}
