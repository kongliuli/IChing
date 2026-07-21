using IChing.Lab.Core.Integrations;

namespace IChing.Tarot.App.Services;

public sealed class AppSettings : IOpenAiChatCredentials
{
    public const string PrefApiKey = "api_key";
    public const string SecureApiKey = "secure_api_key";
    public const string PrefBaseUrl = "api_base_url";
    public const string PrefModel = "api_model";
    public const string PrefProvider = "api_provider";
    public const string PrefLabApiUrl = "lab_api_url";
    public const string PrefUseLabApi = "use_lab_api";
    public const string PrefInterpretTier = "interpret_tier";
    public const string PrefCardCdnBase = "card_cdn_base";
    public const string PrefLocalOnnxPath = "local_onnx_model_path";
    public const string PrefLocalOnnxModelId = "local_onnx_model_id";
    public const string DefaultDeepSeekUrl = "https://api.deepseek.com/v1";

    private string? _apiKeyCache;
    private bool _apiKeyLoaded;
    private bool _secureStorageUnavailable;

    public string ApiKey
    {
        get
        {
            if (!_apiKeyLoaded)
            {
                _apiKeyCache = LoadApiKey();
                _apiKeyLoaded = true;
            }

            return _apiKeyCache ?? string.Empty;
        }
        set
        {
            _apiKeyCache = value ?? string.Empty;
            _apiKeyLoaded = true;
            PersistApiKey(_apiKeyCache);
        }
    }

    /// <summary>SecureStorage 不可用时为 true（设置页可提示）。</summary>
    public bool IsApiKeyUsingFallbackStorage => _secureStorageUnavailable;

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
        get
        {
            var stored = Preferences.Default.Get(PrefLabApiUrl, string.Empty);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                return stored;
            }

            return EditionHost.DefaultLabApiUrl;
        }
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
        get
        {
            var stored = Preferences.Default.Get(PrefCardCdnBase, string.Empty);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                return stored;
            }

            return EditionHost.DefaultCardCdnBase ?? TarotCardImageCache.DefaultCdnBase;
        }
        set => Preferences.Default.Set(PrefCardCdnBase, value.Trim());
    }

    public string LocalOnnxModelId
    {
        get => Preferences.Default.Get(PrefLocalOnnxModelId, IChing.Client.Shared.Onnx.Qwen35ModelCatalog.DefaultDownloadId);
        set => Preferences.Default.Set(PrefLocalOnnxModelId, value);
    }

    public string LocalOnnxModelPath
    {
        get => Preferences.Default.Get(PrefLocalOnnxPath, string.Empty);
        set => Preferences.Default.Set(PrefLocalOnnxPath, value);
    }

    public string ResolveLocalOnnxModelPath()
    {
        if (!string.IsNullOrWhiteSpace(LocalOnnxModelPath) && Directory.Exists(LocalOnnxModelPath))
        {
            return LocalOnnxModelPath;
        }

        var appData = Path.Combine(FileSystem.AppDataDirectory, "models", LocalOnnxModelId);
        if (Directory.Exists(appData) && File.Exists(Path.Combine(appData, "genai_config.json")))
        {
            return appData;
        }

        foreach (var candidate in IChing.Client.Shared.Onnx.OnnxModelPackCatalog.DevRepoModelCandidates(LocalOnnxModelId))
        {
            if (File.Exists(Path.Combine(candidate, "genai_config.json")))
            {
                return candidate;
            }
        }

        return appData;
    }

    public bool IsConfigured =>
        IChing.Lab.Core.Integrations.OpenAiEndpointHelpers.IsConfigured(ApiKey, BaseUrl);

    public bool IsLabConfigured => !string.IsNullOrWhiteSpace(LabApiUrl);

    /// <summary>端侧 ONNX 目录已就绪（含 genai_config.json）。</summary>
    public bool HasLocalOnnxModel
    {
        get
        {
            var path = ResolveLocalOnnxModelPath();
            return !string.IsNullOrWhiteSpace(path)
                   && File.Exists(Path.Combine(path, "genai_config.json"));
        }
    }

    public string AuthToken
    {
        get => Preferences.Default.Get("api_auth_token", string.Empty);
        set => Preferences.Default.Set("api_auth_token", value);
    }

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

    private string LoadApiKey()
    {
        try
        {
            var secure = SecureStorage.Default.GetAsync(SecureApiKey).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(secure))
            {
                return secure;
            }

            // 迁移：Preferences 明文 → SecureStorage
            var legacy = Preferences.Default.Get(PrefApiKey, string.Empty);
            if (!string.IsNullOrEmpty(legacy))
            {
                SecureStorage.Default.SetAsync(SecureApiKey, legacy).GetAwaiter().GetResult();
                Preferences.Default.Remove(PrefApiKey);
                return legacy;
            }

            return string.Empty;
        }
        catch
        {
            _secureStorageUnavailable = true;
            return Preferences.Default.Get(PrefApiKey, string.Empty);
        }
    }

    private void PersistApiKey(string value)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
            {
                SecureStorage.Default.Remove(SecureApiKey);
            }
            else
            {
                SecureStorage.Default.SetAsync(SecureApiKey, value).GetAwaiter().GetResult();
            }

            Preferences.Default.Remove(PrefApiKey);
            _secureStorageUnavailable = false;
        }
        catch
        {
            _secureStorageUnavailable = true;
            Preferences.Default.Set(PrefApiKey, value);
        }
    }
}
