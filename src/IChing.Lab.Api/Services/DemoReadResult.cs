namespace IChing.Lab.Api.Services;

public sealed record DemoReadResult(
    int Tier,
    string OneLiner,
    string? Disclaimer,
    string? Text,
    string? TextEn,
    bool IsFallback,
    string? FallbackReason,
    string? PromptTemplate,
    string? PromptPreview);
