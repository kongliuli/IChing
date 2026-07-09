using System.Diagnostics;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;

namespace IChing.Lab.Tests.Support;

/// <summary>
/// 内存版 IInferenceEngine，供测试验证抽象接口装配。
/// </summary>
public sealed class MockInferenceEngine : IInferenceEngine
{
    public string EngineId => "mock-inference";

    public bool IsReady => true;

    public Task<GenerationResult> GenerateAsync(string prompt, GenerateOptions options, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();
        var text = $"[mock] {prompt}";
        sw.Stop();
        return Task.FromResult(new GenerationResult(
            EngineId: EngineId,
            Text: text,
            IsFallback: false,
            FallbackReason: null,
            ElapsedMs: sw.ElapsedMilliseconds));
    }

    public void Dispose()
    {
    }
}
