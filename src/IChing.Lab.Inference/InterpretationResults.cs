namespace IChing.Lab.Inference;

public record InterpretResult(string Engine, string Text, bool IsFallback, string? TextEn = null);

public record TarotInterpretResult(
    string Engine,
    string TextZh,
    string? TextEn,
    bool IsFallback,
    string? FallbackReason);
