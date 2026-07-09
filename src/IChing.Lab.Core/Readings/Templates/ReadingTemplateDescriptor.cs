namespace IChing.Lab.Core.Readings.Templates;

public sealed record ReadingTemplateDescriptor(
    string TemplateId,
    string Domain,
    string Mode,
    int Tier,
    string OutputSchema,
    string ProducerId,
    bool NeedsTranslationPass = false);

public sealed record TarotTemplateResolution(
    ReadingTemplateDescriptor Descriptor,
    int WordLimit,
    int MaxTokens);
