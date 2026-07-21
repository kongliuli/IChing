using IChing.Client.Shared;
using IChing.Client.Shared.Settings;
using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;
using ProviderResult = IChing.Client.Shared.Providers.InterpretationResult;

namespace IChing.Tarot.App.Services;

/// <summary>
/// 经 CompositeInterpretationProvider；版本能力由 EditionHost 注入。
/// </summary>
public sealed class InterpretationService
{
    private readonly InterpretationFacade _facade;

    public InterpretationService()
    {
        var settings = ClientRuntimeSettingsAdapter.FromAppSettingsLike(
            () => App.Settings.Provider,
            () => App.Settings.BaseUrl,
            () => App.Settings.Model,
            () => App.Settings.ApiKey,
            () => App.Settings.LabApiUrl,
            () => App.Settings.UseLabApi,
            () => App.Settings.InterpretTier,
            () => App.Settings.AuthToken,
            () => 0.6,
            () => OpenAiEndpointHelpers.IsLocalBaseUrl(App.Settings.BaseUrl) ? 1024 : 700,
            () => App.Settings.ResolveLocalOnnxModelPath());
        _facade = new InterpretationFacade(EditionHost.Capabilities, settings);
    }

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
        _ = settings;
        if (tier <= 0)
        {
            return Tier0Result(reading, question);
        }

        var payload = new
        {
            spreadId = reading.SpreadId,
            question = question ?? reading.Question,
            seed = reading.Seed
        };

        var digest = ReadingSummaries.BuildTarotRuleDigest(reading);
        var packet = ReadingPromptPackets.TarotInitial(reading, digest, question);
        var result = await _facade.InterpretTarotAsync(
            payload,
            packet,
            reading,
            digest,
            question ?? reading.Question,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            return Tier0Result(reading, question, result.Error ?? "解读不可用");
        }

        return ToApp(result);
    }

    public Task<ConnectionTestResult> TestConnectionAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        _ = settings;
        return _facade.TestAsync(cancellationToken);
    }

    public IAsyncEnumerable<string> StreamFollowUpAsync(
        IReadOnlyList<ChatTurn> messages,
        CancellationToken cancellationToken = default) =>
        _facade.StreamFollowUpAsync(messages, cancellationToken);

    private static InterpretationResult Tier0Result(TarotReading reading, string? question, string? error = null)
    {
        var text = IChing.Client.Shared.Providers.RichRuleReading.Build(
            "tarot", reading, ReadingSummaries.BuildTarotRuleDigest(reading), question, null);
        return new InterpretationResult(text, true, error);
    }

    private static InterpretationResult ToApp(ProviderResult result) =>
        new(result.Text, result.IsFallback, result.Error);
}
