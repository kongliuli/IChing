using System.Net.Http.Json;
using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Readings;

namespace IChing.Bazi.App.Services;

/// <summary>Tier0 本地规则；Tier1+ 走 Lab API /lab/bazi/read。</summary>
public sealed class BaziInterpretationService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(3) };

    public static InterpretationResult Tier0(BaziChart chart, string? focus)
    {
        var preview = ReadingSummaries.BuildBaziTier0Preview(chart, focus);
        return new InterpretationResult(
            preview.OneLiner + "\n\n" + preview.Disclaimer,
            false,
            null);
    }

    public Task<InterpretationResult> InterpretAsync(
        AppSettings settings,
        BaziChart chart,
        int year, int month, int day, int hour, int minute,
        int? gender,
        int? flowYear,
        string? focus,
        int tier,
        CancellationToken ct = default)
    {
        if (tier <= 0)
        {
            return Task.FromResult(Tier0(chart, focus));
        }

        if (!settings.UseLabApi || !settings.IsLabConfigured)
        {
            var local = Tier0(chart, focus);
            return Task.FromResult(local with { Error = "请在设置中启用 Lab API" });
        }

        return TryLabReadAsync(settings, year, month, day, hour, minute, gender, flowYear, focus, tier, chart, ct);
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        AppSettings settings,
        CancellationToken ct = default)
    {
        if (!settings.IsLabConfigured)
        {
            return new ConnectionTestResult(false, "Lab API 地址为空");
        }

        var url = $"{settings.LabApiUrl.TrimEnd('/')}/health";
        try
        {
            using var response = await Http.GetAsync(url, ct);
            return response.IsSuccessStatusCode
                ? new ConnectionTestResult(true, "Lab API 在线")
                : new ConnectionTestResult(false, $"{(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, ex.Message);
        }
    }

    private static async Task<InterpretationResult> TryLabReadAsync(
        AppSettings settings,
        int year, int month, int day, int hour, int minute,
        int? gender,
        int? flowYear,
        string? focus,
        int tier,
        BaziChart chart,
        CancellationToken ct)
    {
        var url = $"{settings.LabApiUrl.TrimEnd('/')}/lab/bazi/read?tier={tier}";
        var payload = new
        {
            year, month, day, hour, minute,
            gender,
            flowYear,
            focus
        };

        try
        {
            using var response = await Http.PostAsJsonAsync(url, payload, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return Tier0(chart, focus) with { Error = $"Lab {(int)response.StatusCode}" };
            }

            using var doc = JsonDocument.Parse(json);
            var text = TryGetText(doc.RootElement);
            if (string.IsNullOrWhiteSpace(text))
            {
                return Tier0(chart, focus) with { Error = "Lab 响应无文本" };
            }

            var isFallback = doc.RootElement.TryGetProperty("narrative", out var narrative)
                             && narrative.TryGetProperty("isFallback", out var fb)
                             && fb.GetBoolean();
            return new InterpretationResult(text, isFallback, isFallback ? "Lab 降级" : null);
        }
        catch (Exception ex)
        {
            return Tier0(chart, focus) with { Error = ex.Message };
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
}

public sealed record InterpretationResult(string Text, bool IsFallback, string? Error);
public sealed record ConnectionTestResult(bool Ok, string? Error);
