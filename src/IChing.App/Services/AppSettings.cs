namespace IChing.App.Services;

public sealed class AppSettings
{
    public const string DefaultBaseUrl = "https://api.deepseek.com/v1";

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

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public void ApplyProviderPreset(string provider)
    {
        Provider = provider;
        if (provider == "openai")
        {
            BaseUrl = "https://api.openai.com/v1";
            Model = "gpt-4o-mini";
            return;
        }

        if (provider == "deepseek")
        {
            BaseUrl = DefaultBaseUrl;
            Model = "deepseek-chat";
        }
    }
}
