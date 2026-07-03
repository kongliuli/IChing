using IChing.Lab.Abstractions.Models;

namespace IChing.Lab.Abstractions.Engines;

/// <summary>
/// 解读引擎抽象接口，负责根据提示文本生成解读内容。实现方必须可释放。
/// </summary>
public interface IInferenceEngine : IDisposable
{
    /// <summary>引擎标识，唯一区分不同解读引擎实现。</summary>
    string EngineId { get; }

    /// <summary>引擎是否就绪可立即提供服务。</summary>
    bool IsReady { get; }

    /// <summary>
    /// 异步生成解读文本。
    /// </summary>
    /// <param name="prompt">输入提示文本。</param>
    /// <param name="options">生成选项（可覆盖默认参数）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>生成结果，包含文本及执行元信息。</returns>
    Task<GenerationResult> GenerateAsync(string prompt, GenerateOptions options, CancellationToken ct);
}
