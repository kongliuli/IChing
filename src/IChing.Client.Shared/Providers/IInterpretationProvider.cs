using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;

namespace IChing.Client.Shared.Providers;

public sealed record InterpretationResult(string Text, bool IsFallback, string? Error);

/// <summary>
/// 版本差异的唯一扩展点：解读 + 可选追问能力。
/// </summary>
public interface IInterpretationProvider
{
    string ProviderId { get; }

    /// <summary>是否支持 Follow-up（商业/自助）。</summary>
    bool SupportsFollowUp { get; }

    Task<InterpretationResult> InterpretAsync(
        InterpretationRequest request,
        CancellationToken cancellationToken = default);

    Task<ConnectionTestResult> TestAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamFollowUpAsync(
        IReadOnlyList<ChatTurn> messages,
        CancellationToken cancellationToken = default);
}

public sealed record InterpretationRequest(
    string Domain,
    int Tier,
    object? LabReadBody,
    ReadingPromptPacket? FallbackPacket,
    string? Question,
    string? Focus,
    object? Chart,
    object? RuleDigest);
