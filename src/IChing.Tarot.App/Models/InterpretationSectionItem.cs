namespace IChing.Tarot.App.Models;

public sealed class InterpretationSectionItem
{
    public required string Title { get; init; }
    public required string Body { get; init; }
    public bool IsSubsection { get; init; }
    public InterpretationLayoutBand Band { get; init; } = InterpretationLayoutBand.General;
    public Color Accent { get; init; } = Color.FromArgb("#B794F6");
    /// <summary>匹配到的牌位原文（牌名 + 系统牌义）。</summary>
    public string? CardSource { get; init; }
}
