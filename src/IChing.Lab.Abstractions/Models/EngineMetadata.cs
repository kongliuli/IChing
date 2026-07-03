namespace IChing.Lab.Abstractions.Models;

/// <summary>
/// 排盘引擎元数据，描述底层算法来源、版本与算法依据，供 prompt 模板选择与运维诊断使用。
/// </summary>
/// <param name="Source">来源库名，例如 "lunar-csharp" / "IChingLibrary.SixLines" / "builtin"。</param>
/// <param name="Version">来源库版本号，例如 "1.6.8"。</param>
/// <param name="AlgorithmBasis">算法依据描述，例如 "6tail lunar-csharp 0001-9999 年"。</param>
/// <param name="TemplateHint">Prompt 模板变体提示，例如 "lunar" / "cnlunar" / "openfate" / "sixlines"，可为 null。</param>
/// <param name="ModuleFocus">模块面向（如 ["geju","yongshen"]），默认空数组。</param>
public sealed record EngineMetadata(
    string Source,
    string Version,
    string AlgorithmBasis,
    string? TemplateHint,
    IReadOnlyList<string> ModuleFocus)
{
    /// <summary>返回空默认值，用于未提供元数据的实现或测试占位。</summary>
    public static EngineMetadata Default() => new(
        Source: string.Empty,
        Version: string.Empty,
        AlgorithmBasis: string.Empty,
        TemplateHint: null,
        ModuleFocus: Array.Empty<string>());
}
