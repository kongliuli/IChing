using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using IChing.Lab.Core.Readings;

namespace IChing.App.Services;

public sealed class RemoteInterpretationService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(2) };

    public Task<RemoteInterpretationResult> InterpretAsync(
        AppSettings settings,
        string domain,
        ReadingPromptPacket packet,
        CancellationToken cancellationToken = default) =>
        InterpretAsync(
            settings,
            domain,
            ReadingPromptProtocol.BuildUserMessage(packet),
            ReadingPromptProtocol.BuildSystemPrompt(packet),
            packet.Mode,
            cancellationToken);

    public Task<RemoteInterpretationResult> InterpretAsync(
        AppSettings settings,
        string domain,
        string prompt,
        CancellationToken cancellationToken = default) =>
        InterpretAsync(settings, domain, prompt, null, null, cancellationToken);

    private async Task<RemoteInterpretationResult> InterpretAsync(
        AppSettings settings,
        string domain,
        string prompt,
        string? systemPrompt,
        string? mode,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
        {
            return new RemoteInterpretationResult(string.Empty, true, "请先在设置中填写 API Key");
        }

        var structured = prompt.Contains("reading-request.v1", StringComparison.Ordinal);
        var messages = structured
            ? [new ChatTurn("system", systemPrompt ?? ReadingPromptProtocol.SystemPrompt), new ChatTurn("user", prompt)]
            : new[]
            {
                new ChatTurn("system", $"你是谨慎的{domain}解读助手。盘面和规则摘要由系统计算，不能改动已计算事实。"),
                new ChatTurn("user", prompt)
            };

        try
        {
            using var response = await SendAsync(settings, messages, stream: false, cancellationToken, jsonOutput: structured);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RemoteInterpretationResult(string.Empty, true, $"{(int)response.StatusCode}: {Trim(body, 180)}");
            }

            using var doc = JsonDocument.Parse(body);
            LogUsage(doc.RootElement, settings, structured ? domain : null, structured ? mode ?? "initial" : null);
            var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            return new RemoteInterpretationResult(
                structured ? ReadingPromptProtocol.NormalizeOutput(text) : text,
                false,
                null);
        }
        catch (Exception ex)
        {
            return new RemoteInterpretationResult(string.Empty, true, ex.Message);
        }
    }

    public async IAsyncEnumerable<string> StreamAsync(
        AppSettings settings,
        IReadOnlyList<ChatTurn> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
        {
            yield return "请先在设置中填写 API Key";
            yield break;
        }

        using var response = await SendAsync(settings, messages, stream: true, cancellationToken);
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

    public async Task<ConnectionTestResult> TestAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var result = await InterpretAsync(settings, "连通性", "请只回复 pong。", cancellationToken);
        return result.IsFallback ? new ConnectionTestResult(false, result.Error) : new ConnectionTestResult(true, null);
    }

    private static Task<HttpResponseMessage> SendAsync(
        AppSettings settings,
        IReadOnlyList<ChatTurn> messages,
        bool stream,
        CancellationToken cancellationToken,
        bool jsonOutput = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        var payload = new Dictionary<string, object?>
        {
            ["model"] = settings.Model,
            ["messages"] = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            ["temperature"] = settings.Temperature,
            ["max_tokens"] = stream ? Math.Min(settings.MaxTokens, 350) : settings.MaxTokens,
            ["stream"] = stream
        };
        if (jsonOutput)
        {
            payload["response_format"] = new { type = "json_object" };
        }
        if (settings.BaseUrl.Contains("deepseek", StringComparison.OrdinalIgnoreCase))
        {
            payload["thinking"] = new { type = "disabled" };
        }

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        return Http.SendAsync(
            request,
            stream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
            cancellationToken);
    }

    private static string Trim(string text, int max) =>
        text.Length <= max ? text : text[..max] + "...";

    private static void LogUsage(JsonElement root, AppSettings settings, string? domain = null, string? mode = null)
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
                domain,
                mode,
                base_url = settings.BaseUrl,
                model = settings.Model,
                max_tokens = settings.MaxTokens,
                prompt_tokens = GetInt64(usage, "prompt_tokens"),
                completion_tokens = GetInt64(usage, "completion_tokens"),
                total_tokens = GetInt64(usage, "total_tokens"),
                prompt_cache_hit_tokens = GetInt64(usage, "prompt_cache_hit_tokens"),
                prompt_cache_miss_tokens = GetInt64(usage, "prompt_cache_miss_tokens")
            });
            var path = Path.Combine(FileSystem.AppDataDirectory, "deepseek-usage.log");
            File.AppendAllText(path, line + Environment.NewLine);
            System.Diagnostics.Debug.WriteLine($"[DeepSeekUsage] {line}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeepSeekUsage] {ex.Message}");
        }
    }

    private static long? GetInt64(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt64(out var number) ? number : null;
}

public sealed record ChatTurn(string Role, string Content);

public sealed record RemoteInterpretationResult(string Text, bool IsFallback, string? Error);

public sealed record ConnectionTestResult(bool Ok, string? Error);
