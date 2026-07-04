namespace IChing.Tarot.App.Services;

public sealed class AppSettings
{
    public const string PrefApiKey = "api_key";
    public const string PrefBaseUrl = "api_base_url";
    public const string PrefModel = "api_model";
    public const string PrefProvider = "api_provider";
    public const string DefaultDeepSeekUrl = "https://api.deepseek.com/v1";

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

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

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
