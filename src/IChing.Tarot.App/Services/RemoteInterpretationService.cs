using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Engines.Tarot;

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
            return new InterpretationResult(
                Text: preview.OneLiner,
                IsFallback: true,
                Error: "请先在设置中填写 API Key，或启用 Lab API");
        }

        var prompt = BuildDeckauraPrompt(reading, question);
        var endpoint = $"{settings.BaseUrl.TrimEnd('/')}/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = settings.Model,
            messages = new[]
            {
                new { role = "system", content = "你是专业塔罗解读师。牌阵由系统抽取，请勿修改牌名与正逆位，不要编造未列出的牌。" },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = reading.Positions.Count >= 10 ? 900 : 600
        }), Encoding.UTF8, "application/json");

        try
        {
            using var response = await Http.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new InterpretationResult(
                    ReadingSummaries.BuildTarotTier0Preview(reading, question).OneLiner,
                    true,
                    $"{(int)response.StatusCode}: {Trim(json, 200)}");
            }

            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

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
                ex.Message);
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

        var endpoint = $"{settings.BaseUrl.TrimEnd('/')}/chat/completions";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = settings.Model,
            messages = new[] { new { role = "user", content = "ping" } },
            max_tokens = 5
        }), Encoding.UTF8, "application/json");

        try
        {
            using var response = await Http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new ConnectionTestResult(true, null);
            }

            var body = Trim(await response.Content.ReadAsStringAsync(cancellationToken), 120);
            return new ConnectionTestResult(false, $"{(int)response.StatusCode} {body}");
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, ex.Message);
        }
    }

    internal static string BuildDeckauraPrompt(TarotReading reading, string? question)
    {
        var digest = TarotReadingEnricher.BuildEnrichedRuleDigest(reading);
        var digestJson = JsonSerializer.Serialize(digest, new JsonSerializerOptions { WriteIndented = true });

        var sb = new StringBuilder();
        sb.AppendLine($"问题：{question ?? "综合解读"}");
        sb.AppendLine($"牌阵：{reading.SpreadTitleZh}（{reading.SpreadTitle}）");
        sb.AppendLine();
        sb.AppendLine("牌位：");
        foreach (var p in reading.Positions)
        {
            var deckaura = TarotDeckData.FindByNameIgnoreCase(p.CardName);
            sb.AppendLine($"- [{p.PositionTitleZh}] {p.CardName} / {p.CardNameZh} · {(p.Reversed ? "逆位" : "正位")}");
            sb.AppendLine($"  牌义：{p.Meaning}");
            if (deckaura is not null)
            {
                sb.AppendLine($"  关键词：{deckaura.Keywords} · 元素 {deckaura.Element} · Yes/No {deckaura.YesNo}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("规则摘要：");
        sb.AppendLine(digestJson);
        sb.AppendLine();
        sb.AppendLine("请严格用简体中文、Markdown 分段输出（不要对话口吻，不要「你好」等寒暄），结构如下：");
        sb.AppendLine("## 整体能量");
        sb.AppendLine("（2–3 句概括牌阵基调）");
        sb.AppendLine("## 牌位解读");
        sb.AppendLine("### [牌位中文名] 牌名 · 正/逆位");
        sb.AppendLine("（该位置 2–4 句，仅解读已列出的牌）");
        sb.AppendLine("## 牌阵互动");
        sb.AppendLine("（元素/数字/大阿卡纳比例等 2–3 句）");
        sb.AppendLine("## 行动建议");
        sb.AppendLine("（3 条可执行建议，每条一行，以 - 开头）");
        sb.AppendLine($"总字数约 { (reading.Positions.Count >= 10 ? 500 : 350) } 字。");
        return sb.ToString();
    }

    private static string Trim(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}

public sealed record InterpretationResult(string Text, bool IsFallback, string? Error);

public sealed record ConnectionTestResult(bool Ok, string? Error);
