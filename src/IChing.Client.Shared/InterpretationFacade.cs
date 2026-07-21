using IChing.Client.Shared.Editions;
using IChing.Client.Shared.Providers;
using IChing.Client.Shared.Settings;
using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;

namespace IChing.Client.Shared;

/// <summary>
/// App 侧薄门面：把旧 InterpretationService 调用迁到 Provider。
/// </summary>
public sealed class InterpretationFacade
{
    private readonly CompositeInterpretationProvider _provider;

    public InterpretationFacade(EditionCapabilities edition, IClientRuntimeSettings settings)
    {
        Edition = edition;
        Settings = settings;
        _provider = new CompositeInterpretationProvider(edition, settings);
    }

    public EditionCapabilities Edition { get; }
    public IClientRuntimeSettings Settings { get; }
    public IInterpretationProvider Provider => _provider;

    public Task<InterpretationResult> InterpretBaziAsync(
        object labReadBody,
        ReadingPromptPacket fallbackPacket,
        object? chart = null,
        object? ruleDigest = null,
        CancellationToken cancellationToken = default) =>
        _provider.InterpretAsync(
            new InterpretationRequest(
                "bazi",
                Settings.InterpretTier,
                labReadBody,
                fallbackPacket,
                fallbackPacket.Question,
                fallbackPacket.Focus,
                chart,
                ruleDigest),
            cancellationToken);

    public Task<InterpretationResult> InterpretLiuyaoAsync(
        object labReadBody,
        ReadingPromptPacket fallbackPacket,
        object? chart = null,
        object? ruleDigest = null,
        CancellationToken cancellationToken = default) =>
        _provider.InterpretAsync(
            new InterpretationRequest(
                "liuyao",
                Settings.InterpretTier,
                labReadBody,
                fallbackPacket,
                fallbackPacket.Question,
                fallbackPacket.Focus,
                chart,
                ruleDigest),
            cancellationToken);

    public Task<InterpretationResult> InterpretTarotAsync(
        object labReadBody,
        ReadingPromptPacket? fallbackPacket,
        object? chart = null,
        object? ruleDigest = null,
        string? question = null,
        CancellationToken cancellationToken = default) =>
        _provider.InterpretAsync(
            new InterpretationRequest(
                "tarot",
                Settings.InterpretTier,
                labReadBody,
                fallbackPacket,
                question ?? fallbackPacket?.Question,
                fallbackPacket?.Focus,
                chart,
                ruleDigest),
            cancellationToken);

    public Task<ConnectionTestResult> TestAsync(CancellationToken cancellationToken = default) =>
        _provider.TestAsync(cancellationToken);

    public IAsyncEnumerable<string> StreamFollowUpAsync(
        IReadOnlyList<ChatTurn> messages,
        CancellationToken cancellationToken = default) =>
        _provider.StreamFollowUpAsync(messages, cancellationToken);
}
