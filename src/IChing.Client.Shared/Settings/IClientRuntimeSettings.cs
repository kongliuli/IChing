using IChing.Lab.Core.Integrations;

namespace IChing.Client.Shared.Settings;

/// <summary>
/// 与 Preferences 解耦的凭证/端点配置，供 Provider 使用。
/// </summary>
public interface IClientRuntimeSettings : IOpenAiChatCredentials
{
    string LabApiUrl { get; }
    bool UseLabApi { get; }
    int InterpretTier { get; }
    string AuthToken { get; }
    double Temperature { get; }
    int MaxTokens { get; }
    string? LocalOnnxModelPath { get; }
    bool IsLabConfigured { get; }
}

public sealed class MutableClientRuntimeSettings : IClientRuntimeSettings
{
    public string Provider { get; set; } = "deepseek";
    public string BaseUrl { get; set; } = ProviderPresets.DeepSeek.BaseUrl;
    public string Model { get; set; } = ProviderPresets.DeepSeek.Model;
    public string ApiKey { get; set; } = string.Empty;
    public string LabApiUrl { get; set; } = "http://localhost:5000";
    public bool UseLabApi { get; set; }
    public int InterpretTier { get; set; } = 1;
    public string AuthToken { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.6;
    public int MaxTokens { get; set; } = 700;
    public string? LocalOnnxModelPath { get; set; }

    public bool IsConfigured => OpenAiEndpointHelpers.IsConfigured(ApiKey, BaseUrl);
    public bool IsLabConfigured => !string.IsNullOrWhiteSpace(LabApiUrl);
}
