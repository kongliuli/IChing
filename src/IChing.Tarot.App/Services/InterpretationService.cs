using System.Net.Http.Json;
using System.Text.Json;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

/// <summary>
/// 优先 Lab Tier API，其次远程 OpenAI 兼容，最后 Tier0 规则摘要。
/// </summary>
public sealed class InterpretationService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(3) };
    private readonly RemoteInterpretationService _remote = new();

    public Task<InterpretationResult> InterpretAsync(
        AppSettings settings,
        TarotReading reading,
        string? question,
        CancellationToken cancellationToken = default) =>
        InterpretAsync(settings, reading, question, tier: settings.InterpretTier, cancellationToken);

    public async Task<InterpretationResult> InterpretAsync(
        AppSettings settings,
        TarotReading reading,
        string? question,
        int tier,
        CancellationToken cancellationToken = default)
    {
        if (tier <= 0)
        {
            return Tier0Result(reading, question);
        }

        if (settings.UseLabApi && settings.IsLabConfigured)
        {
            var lab = await TryLabReadAsync(settings, reading, question, tier, cancellationToken);
            if (lab is not null)
            {
                return lab;
            }
        }

        if (settings.IsConfigured)
        {
            var remote = await _remote.InterpretAsync(settings, reading, question, cancellationToken);
            if (!remote.IsFallback || string.IsNullOrWhiteSpace(settings.LabApiUrl))
            {
                return remote;
            }
        }

        return Tier0Result(reading, question, settings.IsConfigured ? "Lab 与远程解读均不可用" : "请配置 Lab API 或远程 API Key");
    }

    public Task<ConnectionTestResult> TestConnectionAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.UseLabApi && settings.IsLabConfigured)
        {
            return TestLabAsync(settings, cancellationToken);
        }

        return _remote.TestConnectionAsync(settings, cancellationToken);
    }

    private static InterpretationResult Tier0Result(TarotReading reading, string? question, string? error = null)
    {
        var preview = ReadingSummaries.BuildTarotTier0Preview(reading, question);
        return new InterpretationResult(preview.OneLiner, true, error);
    }

    private static async Task<InterpretationResult?> TryLabReadAsync(
        AppSettings settings,
        TarotReading reading,
        string? question,
        int tier,
        CancellationToken cancellationToken)
    {
        var url = $"{settings.LabApiUrl.TrimEnd('/')}/lab/tarot/read?tier={tier}";
        var payload = new
        {
            spreadId = reading.SpreadId,
            question = question ?? reading.Question,
            seed = reading.Seed
        };

        try
        {
            using var response = await Http.PostAsJsonAsync(url, payload, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var text = TryGetText(root);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var isFallback = root.TryGetProperty("narrative", out var narrative)
                             && narrative.TryGetProperty("isFallback", out var fb)
                             && fb.GetBoolean();
            return new InterpretationResult(text, isFallback, isFallback ? "Lab 降级为模板" : null);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetText(JsonElement root)
    {
        if (root.TryGetProperty("narrative", out var narrative)
            && narrative.TryGetProperty("text", out var textProp)
            && textProp.ValueKind == JsonValueKind.String)
        {
            return textProp.GetString();
        }

        if (root.TryGetProperty("tier0Preview", out var preview)
            && preview.TryGetProperty("oneLiner", out var oneLiner)
            && oneLiner.ValueKind == JsonValueKind.String)
        {
            return oneLiner.GetString();
        }

        return null;
    }

    private static async Task<ConnectionTestResult> TestLabAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{settings.LabApiUrl.TrimEnd('/')}/health";
        try
        {
            using var response = await Http.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode
                ? new ConnectionTestResult(true, "Lab API 在线")
                : new ConnectionTestResult(false, $"{(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, ex.Message);
        }
    }
}
