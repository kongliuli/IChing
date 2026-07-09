using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

public sealed class RemoteInterpretationService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(2) };

    public async Task<InterpretationResult> InterpretAsync(
        AppSettings settings,
        TarotReading reading,
        string? question,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
        {
            var preview = ReadingSummaries.BuildTarotTier0Preview(reading, question);
            return new InterpretationResult(preview.OneLiner, true, "请先在设置中填写 API Key，或启用 Lab API");
        }

        var packet = ReadingPromptPackets.TarotInitial(
            reading,
            ReadingSummaries.BuildTarotRuleDigest(reading),
            question);
        var messages = new[]
        {
            new ChatTurn("system", ReadingPromptProtocol.BuildSystemPrompt(packet)),
            new ChatTurn("user", ReadingPromptProtocol.BuildUserMessage(packet))
        };
        var maxTokens = reading.Positions.Count >= 10 ? 700 : 500;

        try
        {
            using var response = await SendAsync(settings, messages, stream: false, maxTokens, cancellationToken, jsonOutput: true);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new InterpretationResult(
                    ReadingSummaries.BuildTarotTier0Preview(reading, question).OneLiner,
                    true,
                    UserFacingZh.Error($"{(int)response.StatusCode}: {Trim(json, 200)}"));
            }

            using var doc = JsonDocument.Parse(json);
            LogUsage(doc.RootElement, settings, maxTokens, packet.Domain, packet.Mode);
            var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return new InterpretationResult(
                ReadingPromptProtocol.NormalizeOutput(text ?? ReadingSummaries.BuildTarotTier0Preview(reading, question).OneLiner),
                false,
                null);
        }
        catch (Exception ex)
        {
            return new InterpretationResult(
                ReadingSummaries.BuildTarotTier0Preview(reading, question).OneLiner,
                true,
                UserFacingZh.Error(ex.Message));
        }
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
        {
            return new ConnectionTestResult(false, "API Key 为空");
        }

        try
        {
            using var response = await SendAsync(settings, [new ChatTurn("user", "ping")], stream: false, maxTokens: 5, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new ConnectionTestResult(true, null);
            }

            var body = Trim(await response.Content.ReadAsStringAsync(cancellationToken), 120);
            return new ConnectionTestResult(false, UserFacingZh.Error($"{(int)response.StatusCode} {body}"));
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, UserFacingZh.Error(ex.Message));
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

        using var response = await SendAsync(settings, messages, stream: true, maxTokens: 350, cancellationToken);
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

    private static Task<HttpResponseMessage> SendAsync(
        AppSettings settings,
        IReadOnlyList<ChatTurn> messages,
        bool stream,
        int maxTokens,
        CancellationToken cancellationToken,
        bool jsonOutput = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        var payload = new Dictionary<string, object?>
        {
            ["model"] = settings.Model,
            ["messages"] = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            ["temperature"] = 0.7,
            ["max_tokens"] = maxTokens,
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

    private static string Trim(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";

    private static void LogUsage(JsonElement root, AppSettings settings, int maxTokens, string? domain = null, string? mode = null)
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
                max_tokens = maxTokens,
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

public sealed record InterpretationResult(string Text, bool IsFallback, string? Error);

public sealed record ConnectionTestResult(bool Ok, string? Error);

public sealed record ChatTurn(string Role, string Content);
