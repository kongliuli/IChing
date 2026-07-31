using System.Text.Json.Serialization;

namespace IChing.Lab.Core.Readings.Templates;

public sealed record ReadingTemplateSection(string Key, string Title);

public sealed record ReadingTemplateMeta(
    [property: JsonPropertyName("templateId")] string TemplateId,
    [property: JsonPropertyName("domain")] string Domain,
    [property: JsonPropertyName("tier")] int Tier,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("needsTranslationPass")] bool NeedsTranslationPass,
    [property: JsonPropertyName("wordLimit")] int WordLimit,
    [property: JsonPropertyName("maxTokens")] int MaxTokens,
    [property: JsonPropertyName("systemDirectives")] IReadOnlyList<string> SystemDirectives,
    [property: JsonPropertyName("outputSections")] IReadOnlyList<ReadingTemplateSection> OutputSections
);
