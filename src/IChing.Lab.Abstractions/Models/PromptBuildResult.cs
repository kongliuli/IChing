namespace IChing.Lab.Abstractions.Models;

/// <summary>
/// Prompt 构建结果，包含最终拼装好的提示文本及元信息。
/// </summary>
/// <param name="PromptText">拼装完成的提示文本。</param>
/// <param name="EngineHint">建议使用的解读引擎标识（可选）。</param>
/// <param name="NeedsTranslationPass">是否需要额外的翻译处理流程。</param>
public sealed record PromptBuildResult(
    string PromptText,
    string? EngineHint,
    bool NeedsTranslationPass);
