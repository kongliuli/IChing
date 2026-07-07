namespace IChing.Tarot.App.Services;

public sealed class AppSettings
{
    public const string PrefApiKey = "api_key";
    public const string PrefBaseUrl = "api_base_url";
    public const string PrefModel = "api_model";
    public const string PrefProvider = "api_provider";
    public const string PrefLabApiUrl = "lab_api_url";
    public const string PrefUseLabApi = "use_lab_api";
    public const string PrefInterpretTier = "interpret_tier";
    public const string PrefCardCdnBase = "card_cdn_base";
    public const string DefaultDeepSeekUrl = "https://api.deepseek.com/v1";
    public const string DefaultLabApiUrl = "http://localhost:5000";

    public string ApiKey
    {
        get => Preferences.Default.Get(PrefApiKey, string.Empty);
        set => Preferences.Default.Set(PrefApiKey, value);
    }

    public string BaseUrl
    {
        get => Preferences.Default.Get(PrefBaseUrl, DefaultDeepSeekUrl);
        set => Preferences.Default.Set(PrefBaseUrl, value);
    }

    public string Model
    {
        get => Preferences.Default.Get(PrefModel, "deepseek-chat");
        set => Preferences.Default.Set(PrefModel, value);
    }

    public string Provider
    {
        get => Preferences.Default.Get(PrefProvider, "deepseek");
        set => Preferences.Default.Set(PrefProvider, value);
    }

    public string LabApiUrl
    {
        get => Preferences.Default.Get(PrefLabApiUrl, DefaultLabApiUrl);
        set => Preferences.Default.Set(PrefLabApiUrl, value);
    }

    public bool UseLabApi
    {
        get => Preferences.Default.Get(PrefUseLabApi, false);
        set => Preferences.Default.Set(PrefUseLabApi, value);
    }

    public int InterpretTier
    {
        get => Preferences.Default.Get(PrefInterpretTier, 1);
        set => Preferences.Default.Set(PrefInterpretTier, Math.Clamp(value, 0, 2));
    }

    public string CardCdnBaseUrl
    {
        get => Preferences.Default.Get(PrefCardCdnBase, TarotCardImageCache.DefaultCdnBase);
        set => Preferences.Default.Set(PrefCardCdnBase, value.Trim());
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public bool IsLabConfigured => !string.IsNullOrWhiteSpace(LabApiUrl);

    public void ApplyProviderPreset(string provider)
    {
        Provider = provider;
        switch (provider)
        {
            case "openai":
                BaseUrl = "https://api.openai.com/v1";
                Model = "gpt-4o-mini";
                break;
            case "deepseek":
                BaseUrl = "https://api.deepseek.com/v1";
                Model = "deepseek-chat";
                break;
            default:
                break;
        }
    }
}
