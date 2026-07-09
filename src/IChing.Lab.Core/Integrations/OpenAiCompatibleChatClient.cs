using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace IChing.Lab.Core.Integrations;

/// <summary>
/// OpenAI 兼容 <c>/chat/completions</c> HTTP 客户端，供 MAUI App 与远程推理插件共用。
/// </summary>
public static class OpenAiCompatibleChatClient
{
    private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromMinutes(2) };

    public static Task<HttpResponseMessage> SendAsync(
        IOpenAiChatCredentials credentials,
        OpenAiChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{credentials.BaseUrl.TrimEnd('/')}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.ApiKey);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = credentials.Model,
            ["messages"] = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            ["temperature"] = request.Temperature,
            ["max_tokens"] = request.MaxTokens,
            ["stream"] = request.Stream
        };
        if (request.JsonOutput)
        {
            payload["response_format"] = new { type = "json_object" };
        }

        if (credentials.BaseUrl.Contains("deepseek", StringComparison.OrdinalIgnoreCase))
        {
            payload["thinking"] = new { type = "disabled" };
        }

        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return SharedHttp.SendAsync(
            httpRequest,
            request.Stream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
            cancellationToken);
    }

    public static async IAsyncEnumerable<string> StreamContentAsync(
        IOpenAiChatCredentials credentials,
        IReadOnlyList<ChatTurn> messages,
        int maxTokens,
        double temperature = 0.7,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!credentials.IsConfigured)
        {
            yield return "请先在设置中填写 API Key";
            yield break;
        }

        using var response = await SendAsync(
            credentials,
            new OpenAiChatRequest(messages, maxTokens, temperature, Stream: true),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            yield return $"{(int)response.StatusCode}: {Trim(await response.Content.ReadAsStringAsync(cancellationToken), 180)}";
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (data == "[DONE]")
            {
                yield break;
            }

            using var doc = JsonDocument.Parse(data);
            if (doc.RootElement.TryGetProperty("choices", out var choices)
                && choices[0].TryGetProperty("delta", out var delta)
                && delta.TryGetProperty("content", out var content))
            {
                yield return content.GetString() ?? string.Empty;
            }
        }
    }

    public static string? ExtractMessageContent(JsonElement root) =>
        root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

    public static void AppendUsageLog(
        JsonElement root,
        IOpenAiChatCredentials credentials,
        OpenAiChatUsageContext context,
        string logFilePath)
    {
        if (!root.TryGetProperty("usage", out var usage))
        {
            return;
        }

        try
        {
            var line = JsonSerializer.Serialize(new
            {
                at = DateTimeOffset.Now.ToString("O"),
                domain = context.Domain,
                mode = context.Mode,
                base_url = credentials.BaseUrl,
                model = credentials.Model,
                max_tokens = context.MaxTokens,
                prompt_tokens = GetInt64(usage, "prompt_tokens"),
                completion_tokens = GetInt64(usage, "completion_tokens"),
                total_tokens = GetInt64(usage, "total_tokens"),
                prompt_cache_hit_tokens = GetInt64(usage, "prompt_cache_hit_tokens"),
                prompt_cache_miss_tokens = GetInt64(usage, "prompt_cache_miss_tokens")
            });
            File.AppendAllText(logFilePath, line + Environment.NewLine);
            System.Diagnostics.Debug.WriteLine($"[DeepSeekUsage] {line}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeepSeekUsage] {ex.Message}");
        }
    }

    public static string Trim(string text, int max) =>
        text.Length <= max ? text : text[..max] + "...";

    private static long? GetInt64(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt64(out var number) ? number : null;
}
