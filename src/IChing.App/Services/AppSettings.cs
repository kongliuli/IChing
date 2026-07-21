using IChing.Lab.Core.Integrations;

namespace IChing.App.Services;

public sealed class AppSettings : IOpenAiChatCredentials
{
    public const string DefaultBaseUrl = "https://api.deepseek.com/v1";
    public const string DefaultLabApiUrl = "http://localhost:5000";

    public string Provider
    {
        get => Preferences.Default.Get("iching_provider", "deepseek");
        set => Preferences.Default.Set("iching_provider", value);
    }

    public string BaseUrl
    {
        get => Preferences.Default.Get("iching_base_url", DefaultBaseUrl);
        set => Preferences.Default.Set("iching_base_url", value);
    }

    public string Model
    {
        get => Preferences.Default.Get("iching_model", "deepseek-chat");
        set => Preferences.Default.Set("iching_model", value);
    }

    public string ApiKey
    {
        get => Preferences.Default.Get("iching_api_key", string.Empty);
        set => Preferences.Default.Set("iching_api_key", value);
    }

    public string LabApiUrl
    {
        get => Preferences.Default.Get("iching_lab_api_url", DefaultLabApiUrl);
        set => Preferences.Default.Set("iching_lab_api_url", value);
    }

    public bool UseLabApi
    {
        get => Preferences.Default.Get("iching_use_lab_api", false);
        set => Preferences.Default.Set("iching_use_lab_api", value);
    }

    public int InterpretTier
    {
        get => Preferences.Default.Get("iching_interpret_tier", 1);
        set => Preferences.Default.Set("iching_interpret_tier", Math.Clamp(value, 0, 2));
    }

    public string AuthToken
    {
        get => Preferences.Default.Get("iching_auth_token", string.Empty);
        set => Preferences.Default.Set("iching_auth_token", value);
    }

    public double Temperature
    {
        get => Preferences.Default.Get("iching_temperature", 0.6);
        set => Preferences.Default.Set("iching_temperature", Math.Clamp(value, 0, 1));
    }

    public int MaxTokens
    {
        get => Preferences.Default.Get("iching_max_tokens", 700);
        set => Preferences.Default.Set("iching_max_tokens", Math.Clamp(value, 128, 4000));
    }

    public bool IsConfigured =>
        IChing.Lab.Core.Integrations.OpenAiEndpointHelpers.IsConfigured(ApiKey, BaseUrl);

    public bool IsLabConfigured => !string.IsNullOrWhiteSpace(LabApiUrl);

    public void ApplyProviderPreset(string provider)
    {
        var preset = IChing.Client.Shared.Settings.ProviderPresets.Find(provider);
        if (preset is null)
        {
            Provider = provider;
            return;
        }

        Provider = preset.Id;
        BaseUrl = preset.BaseUrl;
        Model = preset.Model;
    }
}
