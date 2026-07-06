using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace IChing.App.Services;

public sealed class RemoteInterpretationService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(2) };

    public async Task<RemoteInterpretationResult> InterpretAsync(
        AppSettings settings,
        string domain,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
        {
            return new RemoteInterpretationResult(string.Empty, true, "请先在设置中填写 API Key");
        }

        var messages = new[]
        {
            new ChatTurn("system", $"你是谨慎的{domain}解读助手。盘面和规则摘要由系统计算，不能改动干支、卦名、爻位、六亲或世应。"),
            new ChatTurn("user", prompt)
        };

        try
        {
            using var response = await SendAsync(settings, messages, stream: false, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RemoteInterpretationResult(string.Empty, true, $"{(int)response.StatusCode}: {Trim(body, 180)}");
            }

            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return new RemoteInterpretationResult(text ?? string.Empty, false, null);
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
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = settings.Model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = settings.Temperature,
            max_tokens = settings.MaxTokens,
            stream
        }), Encoding.UTF8, "application/json");

        return Http.SendAsync(
            request,
            stream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
            cancellationToken);
    }

    private static string Trim(string text, int max) =>
        text.Length <= max ? text : text[..max] + "...";
}

public sealed record ChatTurn(string Role, string Content);

public sealed record RemoteInterpretationResult(string Text, bool IsFallback, string? Error);

public sealed record ConnectionTestResult(bool Ok, string? Error);
