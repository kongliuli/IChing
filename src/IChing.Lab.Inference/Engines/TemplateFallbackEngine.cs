using System.Diagnostics;
using IChing.Lab.Abstractions.Engines;
using LabModels = IChing.Lab.Abstractions.Models;

namespace IChing.Lab.Inference.Engines;

/// <summary>
/// 模板兜底引擎：主引擎不可用时返回基于模板的扩写文本，始终就绪。
/// </summary>
public sealed class TemplateFallbackEngine : IInferenceEngine
{
    /// <inheritdoc />
    public string EngineId => "template-fallback";

    /// <inheritdoc />
    public bool IsReady => true;

    /// <inheritdoc />
    public Task<LabModels.GenerationResult> GenerateAsync(
        string prompt,
        LabModels.GenerateOptions options,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();
        var hint = "ONNX model is not ready; confirm model files exist and retry.";
        var text = $"[template fallback] {hint}";
        sw.Stop();
        return Task.FromResult(new LabModels.GenerationResult(
            EngineId,
            text,
            IsFallback: true,
            FallbackReason: "template fallback",
            sw.ElapsedMilliseconds));
    }

    public void Dispose()
    {
        // 模板引擎无托管资源需释放。
    }
}
