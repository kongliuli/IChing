using IChing.Lab.Client;
using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

/// <summary>
/// 优先 Lab Tier API，其次远程 OpenAI 兼容，最后 Tier0 规则摘要。
/// </summary>
public sealed class InterpretationService
{
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

    public async Task<ConnectionTestResult> TestConnectionAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.UseLabApi && settings.IsLabConfigured)
        {
            return await LabApiClient.HealthAsync(settings.LabApiUrl, cancellationToken)
                ? new ConnectionTestResult(true, "Lab API 在线")
                : new ConnectionTestResult(false, "Lab API 不可达");
        }

        return await _remote.TestConnectionAsync(settings, cancellationToken);
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
        var payload = new
        {
            spreadId = reading.SpreadId,
            question = question ?? reading.Question,
            seed = reading.Seed
        };

        try
        {
            using var doc = await LabApiClient.PostReadAsync(
                settings.LabApiUrl,
                "tarot",
                tier,
                payload,
                settings.AuthToken,
                cancellationToken);
            if (doc is null)
            {
                return null;
            }

            var text = LabReadResponseParser.TryGetNarrativeText(doc.RootElement);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var isFallback = LabReadResponseParser.TryGetIsFallback(doc.RootElement);
            return new InterpretationResult(
                ReadingPromptProtocol.NormalizeOutput(text),
                isFallback,
                isFallback ? "Lab 降级为模板" : null);
        }
        catch
        {
            return null;
        }
    }
}
