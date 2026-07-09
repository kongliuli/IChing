namespace IChing.Lab.Inference.Prompts;

public record TarotPositionPrompt(
    string PositionTitle,
    string PositionContext,
    string CardName,
    bool Reversed,
    string MeaningEn);
