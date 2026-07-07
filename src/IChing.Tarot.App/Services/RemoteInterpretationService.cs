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

        var prompt = BuildTarotPrompt(reading, question);
        var messages = new[]
        {
            new ChatTurn("system", "你是专业塔罗解读师。牌阵由系统抽取，请勿修改牌名与正逆位，不要编造未列出的牌。"),
            new ChatTurn("user", prompt)
        };

        try
        {
            using var response = await SendAsync(settings, messages, stream: false, maxTokens: reading.Positions.Count >= 10 ? 900 : 600, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new InterpretationResult(
                    ReadingSummaries.BuildTarotTier0Preview(reading, question).OneLiner,
                    true,
                    UserFacingZh.Error($"{(int)response.StatusCode}: {Trim(json, 200)}"));
            }

            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return new InterpretationResult(
                text ?? ReadingSummaries.BuildTarotTier0Preview(reading, question).OneLiner,
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

        using var response = await SendAsync(settings, messages, stream: true, maxTokens: 600, cancellationToken);
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

    internal static string BuildTarotPrompt(TarotReading reading, string? question)
    {
        var digestJson = JsonSerializer.Serialize(
            TarotReadingStats.BuildRuleDigest(reading),
            new JsonSerializerOptions { WriteIndented = true });
        var sb = new StringBuilder();
        sb.AppendLine($"问题：{question ?? "综合解读"}");
        sb.AppendLine($"牌阵：{reading.SpreadTitleZh}（{reading.SpreadTitle}）");
        sb.AppendLine();
        sb.AppendLine("牌位：");
        foreach (var p in reading.Positions)
        {
            sb.AppendLine($"- [{p.PositionTitleZh}] {p.CardName} / {p.CardNameZh} · {(p.Reversed ? "逆位" : "正位")}");
            sb.AppendLine($"  牌义：{p.Meaning}");
        }

        sb.AppendLine();
        sb.AppendLine("规则摘要：");
        sb.AppendLine(digestJson);
        sb.AppendLine();
        sb.AppendLine("请严格用简体中文、Markdown 分段输出：整体能量、牌位解读、牌阵互动、行动建议。不要寒暄，不要新增牌。");
        return sb.ToString();
    }

    private static Task<HttpResponseMessage> SendAsync(
        AppSettings settings,
        IReadOnlyList<ChatTurn> messages,
        bool stream,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = settings.Model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = 0.7,
            max_tokens = maxTokens,
            stream
        }), Encoding.UTF8, "application/json");

        return Http.SendAsync(
            request,
            stream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
            cancellationToken);
    }

    private static string Trim(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}

public sealed record InterpretationResult(string Text, bool IsFallback, string? Error);

public sealed record ConnectionTestResult(bool Ok, string? Error);

public sealed record ChatTurn(string Role, string Content);
