using System.Diagnostics;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;

namespace IChing.Lab.Abstractions.Tests;

/// <summary>
/// 内存版 IInferenceEngine 模拟实现，用于验证抽象接口可被外部实现并编译通过。
/// 不依赖任何真实模型，仅回显提示文本。
/// </summary>
public sealed class MockInferenceEngine : IInferenceEngine
{
    /// <inheritdoc />
    public string EngineId => "mock-inference";

    /// <inheritdoc />
    public bool IsReady => true;

    /// <inheritdoc />
    public Task<GenerationResult> GenerateAsync(string prompt, GenerateOptions options, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();
        var text = $"[mock] {prompt}";
        sw.Stop();
        var result = new GenerationResult(
            EngineId: EngineId,
            Text: text,
            IsFallback: false,
            FallbackReason: null,
            ElapsedMs: sw.ElapsedMilliseconds);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // 内存 mock 无需释放资源。
    }
}
