namespace IChing.Lab.Abstractions.Readings;

public sealed record ExchangeMeta(
    string Schema,
    string ExchangeId,
    string? SessionId,
    string? ParentExchangeId,
    string Domain,
    string Mode,
    int Tier,
    string Language,
    DateTimeOffset CreatedAt);

public sealed record ExchangePluginContext(
    string PluginId,
    IReadOnlyList<string> Facts,
    IReadOnlyList<string> OutputSections);

public sealed record ExchangeInput(
    string? Question,
    string? Focus,
    IReadOnlyList<string> ComputedFacts,
    IReadOnlyList<string> RuleDigest,
    IReadOnlyList<ExchangePluginContext> PluginContext,
    object? ChartRef = null);

public sealed record DialogueTurn(string Role, string Content);

public sealed record ExchangeDialogue(
    IReadOnlyList<DialogueTurn> History,
    string? UserQuestion,
    int MaxRounds = 3);

public sealed record ExchangeSectionSpec(string Key, string Title);

public sealed record ExchangeRenderSpec(
    string OutputSchema,
    IReadOnlyList<ExchangeSectionSpec> OutputSections,
    IReadOnlyList<string> SystemDirectives,
    string? PromptTemplateId,
    string? PromptProfile);

public sealed record ReadingOutputMeta(string? Confidence, string? Disclaimer);

public sealed record ReadingStructuredSection(string Key, string Title, string Body);

public sealed record ReadingStructuredOutput(
    string Schema,
    string Summary,
    IReadOnlyList<ReadingStructuredSection> Sections,
    IReadOnlyList<string> Warnings,
    ReadingOutputMeta? Meta = null);

public sealed record ExchangeOutput(
    ReadingStructuredOutput? Structured,
    string? RawText,
    string? TextEn,
    string EngineId,
    bool IsFallback,
    string? FallbackReason = null,
    string? PromptTemplateId = null);

public sealed record ReadingExchange(
    ExchangeMeta Meta,
    ExchangeInput Input,
    ExchangeRenderSpec Render,
    ExchangeDialogue? Dialogue = null,
    ExchangeOutput? Output = null);

public sealed record Tier0PreviewDto(string OneLiner, string Disclaimer);

public sealed record ReadingEnvelopeV2(
    string Schema,
    string? SessionId,
    ReadingExchange Exchange,
    object Chart,
    Tier0PreviewDto Tier0Preview);
