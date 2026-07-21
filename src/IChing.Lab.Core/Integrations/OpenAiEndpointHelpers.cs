namespace IChing.Lab.Core.Integrations;

/// <summary>
/// OpenAI 兼容端点判定：本机 Ollama 等无需 API Key。
/// </summary>
public static class OpenAiEndpointHelpers
{
    public static bool IsLocalBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.IsLoopback;
    }

    /// <summary>有 Key，或 BaseUrl 指向本机（localhost / 127.0.0.1 / ::1）。</summary>
    public static bool IsConfigured(string? apiKey, string? baseUrl) =>
        !string.IsNullOrWhiteSpace(apiKey) || IsLocalBaseUrl(baseUrl);
}
