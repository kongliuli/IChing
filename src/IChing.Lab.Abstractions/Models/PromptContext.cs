namespace IChing.Lab.Abstractions.Models;

/// <summary>
/// Prompt 构建上下文，承载排盘结果、规则摘要、用户问题等信息。
/// </summary>
/// <param name="Chart">排盘结果对象，由 IChartEngine.Calculate 返回。</param>
/// <param name="RuleDigest">规则摘要（可选），用于辅助 Prompt 生成。</param>
/// <param name="Question">用户原始问题（可选）。</param>
/// <param name="Focus">本次解读的聚焦点（可选）。</param>
/// <param name="MaxTokens">期望生成的最大 token 数量。</param>
/// <param name="Engine">排盘引擎元数据（可选），用于算法感知的模板选择；旧调用方不传时为 null，行为不变。</param>
/// <param name="ModuleFocuses">本次解读启用的模块面向列表（可选，如 ["geju","yongshen"]）；
/// 旧调用方不传时为 null，由调用方判断 null 时取空数组。</param>
public sealed record PromptContext(
    object Chart,
    object? RuleDigest,
    string? Question,
    string? Focus,
    int MaxTokens,
    EngineMetadata? Engine = null,
    IReadOnlyList<string>? ModuleFocuses = null);
