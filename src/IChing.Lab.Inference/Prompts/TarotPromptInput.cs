namespace IChing.Lab.Inference.Prompts;

/// <summary>
/// 塔罗 Prompt 构建输入，作为 <see cref="IChing.Lab.Abstractions.Models.PromptContext.Chart"/> 传入
/// TemplatePromptBuilder（tarot-tier1-en），承载牌阵标题、牌位与字数上限。
/// </summary>
public sealed record TarotPromptInput(
    string SpreadTitle,
    IReadOnlyList<TarotPositionPrompt> Positions,
    int WordLimit);
