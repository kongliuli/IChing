namespace IChing.Tarot.App.Services;

/// <summary>App 侧解读结果（与 Client.Shared.Providers.InterpretationResult 同形，便于 UI 解耦）。</summary>
public sealed record InterpretationResult(string Text, bool IsFallback, string? Error);
