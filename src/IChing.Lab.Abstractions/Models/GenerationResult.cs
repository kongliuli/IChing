namespace IChing.Lab.Abstractions.Models;

/// <summary>
/// 解读引擎生成结果，承载生成的文本及执行元信息。
/// </summary>
/// <param name="EngineId">实际生成文本的解读引擎标识。</param>
/// <param name="Text">生成的文本内容。</param>
/// <param name="IsFallback">是否为降级回退结果（例如主引擎不可用时使用兜底逻辑）。</param>
/// <param name="FallbackReason">降级回退原因（可选，仅在 IsFallback 为 true 时有意义）。</param>
/// <param name="ElapsedMs">本次生成耗时（毫秒）。</param>
public sealed record GenerationResult(
    string EngineId,
    string Text,
    bool IsFallback,
    string? FallbackReason,
    long ElapsedMs);
