namespace IChing.Lab.Abstractions.Models;

/// <summary>
/// 解读引擎生成调用选项，允许调用方覆盖默认生成参数。
/// </summary>
/// <param name="MaxTokens">最大生成 token 数，默认 512。</param>
/// <param name="EngineHint">指定使用的解读引擎标识（可选）。</param>
/// <param name="Temperature">采样温度（可选）。</param>
/// <param name="TopK">Top-K 采样参数（可选）。</param>
/// <param name="TopP">Top-P（核采样）参数（可选）。</param>
public sealed record GenerateOptions(
    int MaxTokens = 512,
    string? EngineHint = null,
    float? Temperature = null,
    int? TopK = null,
    float? TopP = null);
