using IChing.Client.Shared.Settings;
using IChing.Lab.Core.Integrations;

namespace IChing.Client.Shared.Settings;

/// <summary>
/// 把 Preferences 版 AppSettings 适配为 IClientRuntimeSettings。
/// </summary>
public sealed class ClientRuntimeSettingsAdapter : IClientRuntimeSettings
{
    private readonly Func<string> _getProvider;
    private readonly Func<string> _getBaseUrl;
    private readonly Func<string> _getModel;
    private readonly Func<string> _getApiKey;
    private readonly Func<string> _getLabApiUrl;
    private readonly Func<bool> _getUseLabApi;
    private readonly Func<int> _getTier;
    private readonly Func<string> _getAuthToken;
    private readonly Func<double> _getTemperature;
    private readonly Func<int> _getMaxTokens;
    private readonly Func<string?> _getLocalOnnxPath;

    public ClientRuntimeSettingsAdapter(
        Func<string> getProvider,
        Func<string> getBaseUrl,
        Func<string> getModel,
        Func<string> getApiKey,
        Func<string> getLabApiUrl,
        Func<bool> getUseLabApi,
        Func<int> getTier,
        Func<string> getAuthToken,
        Func<double> getTemperature,
        Func<int> getMaxTokens,
        Func<string?>? getLocalOnnxPath = null)
    {
        _getProvider = getProvider;
        _getBaseUrl = getBaseUrl;
        _getModel = getModel;
        _getApiKey = getApiKey;
        _getLabApiUrl = getLabApiUrl;
        _getUseLabApi = getUseLabApi;
        _getTier = getTier;
        _getAuthToken = getAuthToken;
        _getTemperature = getTemperature;
        _getMaxTokens = getMaxTokens;
        _getLocalOnnxPath = getLocalOnnxPath ?? (() => null);
    }

    public static ClientRuntimeSettingsAdapter FromAppSettingsLike(
        Func<string> getProvider,
        Func<string> getBaseUrl,
        Func<string> getModel,
        Func<string> getApiKey,
        Func<string> labApiUrl,
        Func<bool> useLabApi,
        Func<int> tier,
        Func<string> authToken,
        Func<double> temperature,
        Func<int> maxTokens,
        Func<string?>? localOnnxPath = null) =>
        new(
            getProvider,
            getBaseUrl,
            getModel,
            getApiKey,
            labApiUrl,
            useLabApi,
            tier,
            authToken,
            temperature,
            maxTokens,
            localOnnxPath);

    public string Provider => _getProvider();
    public string BaseUrl => _getBaseUrl();
    public string Model => _getModel();
    public string ApiKey => _getApiKey();
    public string LabApiUrl => _getLabApiUrl();
    public bool UseLabApi => _getUseLabApi();
    public int InterpretTier => _getTier();
    public string AuthToken => _getAuthToken();
    public double Temperature => _getTemperature();
    public int MaxTokens => _getMaxTokens();
    public string? LocalOnnxModelPath => _getLocalOnnxPath();
    public bool IsConfigured => OpenAiEndpointHelpers.IsConfigured(ApiKey, BaseUrl);
    public bool IsLabConfigured => !string.IsNullOrWhiteSpace(LabApiUrl);
}
