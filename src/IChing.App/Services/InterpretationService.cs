using IChing.Lab.Client;
using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;

namespace IChing.App.Services;

/// <summary>
/// 八字/六爻解读：优先 Lab Tier API，其次远程 OpenAI 兼容 HTTP。
/// </summary>
public sealed class InterpretationService
{
    private readonly RemoteInterpretationService _remote = new();

    public async Task<RemoteInterpretationResult> InterpretBaziAsync(
        AppSettings settings,
        object labReadBody,
        ReadingPromptPacket fallbackPacket,
        CancellationToken cancellationToken = default)
    {
        var tier = settings.InterpretTier;
        if (tier <= 0)
        {
            return new RemoteInterpretationResult(string.Empty, true, "Tier 0 请查看规则摘要");
        }

        if (settings.UseLabApi && settings.IsLabConfigured)
        {
            var lab = await TryLabReadAsync(settings, "bazi", tier, labReadBody, cancellationToken);
            if (lab is not null)
            {
                return lab;
            }
        }

        if (settings.IsConfigured)
        {
            return await _remote.InterpretAsync(settings, "八字", fallbackPacket, cancellationToken);
        }

        return new RemoteInterpretationResult(string.Empty, true, "请配置 Lab API 或远程 API Key");
    }

    public async Task<RemoteInterpretationResult> InterpretLiuyaoAsync(
        AppSettings settings,
        object labReadBody,
        ReadingPromptPacket fallbackPacket,
        CancellationToken cancellationToken = default)
    {
        var tier = settings.InterpretTier;
        if (tier <= 0)
        {
            return new RemoteInterpretationResult(string.Empty, true, "Tier 0 请查看规则摘要");
        }

        if (settings.UseLabApi && settings.IsLabConfigured)
        {
            var lab = await TryLabReadAsync(settings, "liuyao", tier, labReadBody, cancellationToken);
            if (lab is not null)
            {
                return lab;
            }
        }

        if (settings.IsConfigured)
        {
            return await _remote.InterpretAsync(settings, "六爻", fallbackPacket, cancellationToken);
        }

        return new RemoteInterpretationResult(string.Empty, true, "请配置 Lab API 或远程 API Key");
    }

    public async Task<ConnectionTestResult> TestAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings.UseLabApi && settings.IsLabConfigured)
        {
            return await LabApiClient.HealthAsync(settings.LabApiUrl, cancellationToken)
                ? new ConnectionTestResult(true, "Lab API 在线")
                : new ConnectionTestResult(false, "Lab API 不可达");
        }

        return await _remote.TestAsync(settings, cancellationToken);
    }

    private static async Task<RemoteInterpretationResult?> TryLabReadAsync(
        AppSettings settings,
        string domain,
        int tier,
        object body,
        CancellationToken cancellationToken)
    {
        try
        {
            using var doc = await LabApiClient.PostReadAsync(
                settings.LabApiUrl,
                domain,
                tier,
                body,
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
            return new RemoteInterpretationResult(
                ReadingPromptProtocol.NormalizeOutput(text),
                isFallback,
                isFallback ? "Lab 降级" : null);
        }
        catch
        {
            return null;
        }
    }
}
