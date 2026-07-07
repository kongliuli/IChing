using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Api.Controllers;
using IChing.Lab.Core.Rules;
using IChing.Lab.Core.Engines;
using IChing.Lab.Core.Services;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Engines;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
// 同时存在 IChing.Lab.Inference.GenerationResult 与 IChing.Lab.Abstractions.Models.GenerationResult，
// 此处以别名显式绑定到抽象层模型，避免 CS0104 歧义。
using GenerationResult = IChing.Lab.Abstractions.Models.GenerationResult;
using GenerateOptions = IChing.Lab.Abstractions.Models.GenerateOptions;

namespace IChing.Lab.Tests;

/// <summary>
/// 降级链编排与健康检查端点的单元测试。
/// 沙箱无 Ollama/OpenAI 实际服务，降级链与端点逻辑均以 mock 引擎验证。
/// </summary>
public class OrchestratorFallbackTests
{
    private static ILogger<ChartInterpretationOrchestrator> OrchestratorLogger =>
        NullLogger<ChartInterpretationOrchestrator>.Instance;

    /// <summary>构造内存 IConfiguration：将 fallbackChain 数组写入 plugins:fallbackChain:i。</summary>
    private static IConfiguration BuildFallbackConfig(params string[] chain)
    {
        var dict = new Dictionary<string, string?>();
        for (var i = 0; i < chain.Length; i++)
        {
            dict[$"plugins:fallbackChain:{i}"] = chain[i];
        }
        return new ConfigurationBuilder().Add(new InMemorySource(dict)).Build();
    }

    /// <summary>构造内存 IConfiguration：含 plugins:inferenceEngines 列表与 default 标记，供健康端点判断默认引擎。</summary>
    private static IConfiguration BuildHealthConfig()
    {
        var dict = new Dictionary<string, string?>
        {
            ["plugins:inferenceEngines:0:id"] = "onnx-genai-qwen2.5-1.5b",
            ["plugins:inferenceEngines:0:default"] = "true",
            ["plugins:inferenceEngines:1:id"] = "ollama-local",
            ["plugins:inferenceEngines:1:default"] = "false"
        };
        return new ConfigurationBuilder().Add(new InMemorySource(dict)).Build();
    }

    private static IConfiguration BuildHealthConfigWithDeepSeek()
    {
        var dict = new Dictionary<string, string?>
        {
            ["plugins:inferenceEngines:0:id"] = "onnx-genai-qwen2.5-1.5b",
            ["plugins:inferenceEngines:0:default"] = "true",
            ["plugins:inferenceEngines:1:id"] = "deepseek-remote",
            ["plugins:inferenceEngines:1:default"] = "false"
        };
        return new ConfigurationBuilder().Add(new InMemorySource(dict)).Build();
    }

    /// <summary>
    /// 降级链应跳过未就绪与抛异常的引擎，使用第一个成功的引擎。
    /// A 未就绪 / B 就绪但抛异常 / C 就绪且成功 / 末位 template-fallback 兜底（不应触达）。
    /// </summary>
    [Fact]
    public async Task FallbackChain_SkipsNotReadyAndThrowing_EngineC_Succeeds()
    {
        var a = new FakeEngine("engine-a", isReady: false);
        var b = new FakeEngine("engine-b", isReady: true, throwOnGenerate: true);
        var c = new FakeEngine("engine-c", isReady: true, text: "from-c");
        var fallback = new TemplateFallbackEngine();
        var engines = new IInferenceEngine[] { a, b, c, fallback };
        var config = BuildFallbackConfig("engine-a", "engine-b", "engine-c", "template-fallback");

        var orchestrator = new ChartInterpretationOrchestrator(
            engines, Enumerable.Empty<IPromptBuilder>(), config, OrchestratorLogger);

        var result = await orchestrator.GenerateWithFallbackAsync(
            "prompt", new GenerateOptions(MaxTokens: 64), CancellationToken.None);

        Assert.Equal("engine-c", result.EngineId);
        Assert.False(result.IsFallback);
        Assert.Equal("from-c", result.Text);
    }

    /// <summary>所有引擎均不可用时，应使用 template-fallback 兜底，IsFallback=true 且 FallbackReason 描述失败明细。</summary>
    [Fact]
    public async Task FallbackChain_AllNotReady_UsesTemplateFallback()
    {
        var a = new FakeEngine("engine-a", isReady: false);
        var b = new FakeEngine("engine-b", isReady: false);
        var fallback = new TemplateFallbackEngine();
        var engines = new IInferenceEngine[] { a, b, fallback };
        var config = BuildFallbackConfig("engine-a", "engine-b", "template-fallback");

        var orchestrator = new ChartInterpretationOrchestrator(
            engines, Enumerable.Empty<IPromptBuilder>(), config, OrchestratorLogger);

        var result = await orchestrator.GenerateWithFallbackAsync(
            "prompt", new GenerateOptions(MaxTokens: 64), CancellationToken.None);

        Assert.Equal("template-fallback", result.EngineId);
        Assert.True(result.IsFallback);
        Assert.Contains("engine-a", result.FallbackReason ?? string.Empty);
        Assert.Contains("engine-b", result.FallbackReason ?? string.Empty);
    }

    /// <summary>未配置 fallbackChain 时应回退到默认链 [onnx-genai-..., template-fallback] 并兜底，不报错。</summary>
    [Fact]
    public async Task FallbackChain_EmptyConfig_UsesDefaultChainAndFallsBack()
    {
        var fallback = new TemplateFallbackEngine();
        var engines = new IInferenceEngine[] { fallback };
        var config = new ConfigurationBuilder().Build(); // 空配置

        var orchestrator = new ChartInterpretationOrchestrator(
            engines, Enumerable.Empty<IPromptBuilder>(), config, OrchestratorLogger);

        var result = await orchestrator.GenerateWithFallbackAsync(
            "prompt", new GenerateOptions(MaxTokens: 64), CancellationToken.None);

        Assert.Equal("template-fallback", result.EngineId);
        Assert.True(result.IsFallback);
    }

    /// <summary>Interpret 内部应走降级链：默认引擎未就绪时自动降级到 template-fallback。</summary>
    [Fact]
    public void Interpret_UsesFallbackChain()
    {
        var notReady = new FakeEngine("onnx-genai-qwen2.5-1.5b", isReady: false);
        var fallback = new TemplateFallbackEngine();
        var engines = new IInferenceEngine[] { notReady, fallback };
        var config = new ConfigurationBuilder().Build();

        var orchestrator = new ChartInterpretationOrchestrator(
            engines, Enumerable.Empty<IPromptBuilder>(), config, OrchestratorLogger);

        var result = orchestrator.Interpret(new { x = 1 }, focus: "test", maxTokens: 32);

        Assert.Equal("template-fallback", result.Engine);
        Assert.True(result.IsFallback);
    }

    /// <summary>/health/engines 应返回所有引擎的 engineId / isReady / isDefault，默认引擎标记正确。</summary>
    [Fact]
    public void HealthEngines_ReturnsAllEnginesWithReadyAndDefault()
    {
        var onnx = new FakeEngine("onnx-genai-qwen2.5-1.5b", isReady: true);
        var ollama = new FakeEngine("ollama-local", isReady: false);
        var inferenceEngines = new IInferenceEngine[] { onnx, ollama };
        var config = BuildHealthConfig();

        var orchestrator = new ChartInterpretationOrchestrator(
            inferenceEngines, Enumerable.Empty<IPromptBuilder>(), config, OrchestratorLogger);

        var chartRouter = new ChartEngineRouter(Enumerable.Empty<IChartEngine>());
        var controller = new LabController(
            orchestrator,
            chartRouter,
            Enumerable.Empty<IChartEngine>(),
            Enumerable.Empty<IPromptBuilder>(),
            inferenceEngines,
            config,
            new RuleEngine());

        var actionResult = controller.HealthEngines();
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var items = Assert.IsAssignableFrom<IReadOnlyList<EngineHealthStatus>>(ok.Value);

        Assert.Equal(2, items.Count);
        Assert.Equal("onnx-genai-qwen2.5-1.5b", items[0].EngineId);
        Assert.True(items[0].IsReady);
        Assert.True(items[0].IsDefault);
        Assert.Equal("ollama-local", items[1].EngineId);
        Assert.False(items[1].IsReady);
        Assert.False(items[1].IsDefault);
    }

    /// <summary>/health/engines 应包含 deepseek-remote（降级链远程引擎）。</summary>
    [Fact]
    public void HealthEngines_IncludesDeepSeekRemote()
    {
        var onnx = new FakeEngine("onnx-genai-qwen2.5-1.5b", isReady: false);
        var deepseek = new FakeEngine("deepseek-remote", isReady: true);
        var inferenceEngines = new IInferenceEngine[] { onnx, deepseek };
        var config = BuildHealthConfigWithDeepSeek();

        var orchestrator = new ChartInterpretationOrchestrator(
            inferenceEngines, Enumerable.Empty<IPromptBuilder>(), config, OrchestratorLogger);

        var chartRouter = new ChartEngineRouter(Enumerable.Empty<IChartEngine>());
        var controller = new LabController(
            orchestrator,
            chartRouter,
            Enumerable.Empty<IChartEngine>(),
            Enumerable.Empty<IPromptBuilder>(),
            inferenceEngines,
            config,
            new RuleEngine());

        var actionResult = controller.HealthEngines();
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var items = Assert.IsAssignableFrom<IReadOnlyList<EngineHealthStatus>>(ok.Value);

        Assert.Contains(items, i => i.EngineId == "deepseek-remote" && i.IsReady);
    }

    /// <summary>bazi 与 calendar 共用 EngineId 时，Orchestrator 按 domain 解析 metadata 不冲突。</summary>
    [Fact]
    public void ResolveEngineMetadata_DistinctByDomain_WhenEngineIdShared()
    {
        var engines = new IChartEngine[] { new BaziChartEngine(), new CalendarEngine() };
        var orchestrator = new ChartInterpretationOrchestrator(
            Enumerable.Empty<IInferenceEngine>(),
            Enumerable.Empty<IPromptBuilder>(),
            new ConfigurationBuilder().Build(),
            OrchestratorLogger,
            engines);

        var baziMeta = orchestrator.ResolveEngineMetadata("bazi", "lunar-csharp-1.6.8");
        var calendarMeta = orchestrator.ResolveEngineMetadata("calendar", "lunar-csharp-1.6.8");

        Assert.NotNull(baziMeta);
        Assert.NotNull(calendarMeta);
        Assert.Contains("geju", baziMeta!.ModuleFocus);
        Assert.Contains("huangli", calendarMeta!.ModuleFocus);
    }

    /// <summary>/health 简单存活探活应返回 200 Ok。</summary>
    [Fact]
    public void Health_ReturnsOk()
    {
        var config = new ConfigurationBuilder().Build();
        var orchestrator = new ChartInterpretationOrchestrator(
            Enumerable.Empty<IInferenceEngine>(), Enumerable.Empty<IPromptBuilder>(), config, OrchestratorLogger);

        var chartRouter = new ChartEngineRouter(Enumerable.Empty<IChartEngine>());
        var controller = new LabController(
            orchestrator,
            chartRouter,
            Enumerable.Empty<IChartEngine>(),
            Enumerable.Empty<IPromptBuilder>(),
            Enumerable.Empty<IInferenceEngine>(),
            config,
            new RuleEngine());

        var actionResult = controller.Health();
        Assert.IsType<OkObjectResult>(actionResult);
    }

    /// <summary>可配置的 IInferenceEngine 模拟实现，用于验证降级链逻辑，不依赖任何真实模型。</summary>
    private sealed class FakeEngine : IInferenceEngine
    {
        private readonly string _text;
        private readonly bool _throwOnGenerate;
        private readonly bool _fallback;

        public FakeEngine(
            string engineId,
            bool isReady,
            string text = "ok",
            bool throwOnGenerate = false,
            bool fallback = false)
        {
            EngineId = engineId;
            IsReady = isReady;
            _text = text;
            _throwOnGenerate = throwOnGenerate;
            _fallback = fallback;
        }

        public string EngineId { get; }
        public bool IsReady { get; }

        public Task<GenerationResult> GenerateAsync(string prompt, GenerateOptions options, CancellationToken ct)
        {
            if (_throwOnGenerate)
            {
                throw new InvalidOperationException($"boom-{EngineId}");
            }

            return Task.FromResult(new GenerationResult(
                EngineId, _text, _fallback, _fallback ? "internal fallback" : null, 1));
        }

        public void Dispose()
        {
            // 内存 mock 无需释放资源。
        }
    }

    /// <summary>最小内存配置源，提供预置键值（替代不可用的 Configuration.Memory 包）。</summary>
    private sealed class InMemorySource : IConfigurationSource
    {
        private readonly IDictionary<string, string?> _data;
        public InMemorySource(IDictionary<string, string?> data) => _data = data;
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new InMemoryProvider(_data);
    }

    private sealed class InMemoryProvider : ConfigurationProvider
    {
        public InMemoryProvider(IDictionary<string, string?> data)
        {
            foreach (var kv in data)
            {
                Data[kv.Key] = kv.Value;
            }
        }
    }
}
