using IChing.Client.Shared.Onnx;
using IChing.Client.Shared.Settings;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;
using IChing.Lab.Inference.Engines;
using IChing.Client.Shared.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IChing.Client.Shared.Providers;

/// <summary>
/// 端侧 ORT GenAI。Android / Windows 可用；iOS 当前 ORT GenAI 未就绪，自动降级规则解读。
/// </summary>
public sealed class LocalOnnxProvider : IInterpretationProvider, IDisposable
{
    private readonly IClientRuntimeSettings _settings;
    private readonly RuleOnlyProvider _fallback = new();
    private readonly ILogger<OnnxGenAiEngine> _logger;
    private OnnxGenAiEngine? _engine;

    public LocalOnnxProvider(
        IClientRuntimeSettings settings,
        ILogger<OnnxGenAiEngine>? logger = null)
    {
        _settings = settings;
        _logger = logger ?? NullLogger<OnnxGenAiEngine>.Instance;
    }

    public string ProviderId => "local-onnx";
    public bool SupportsFollowUp => false;

    public bool IsPlatformSupported =>
        !OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst();

    public async Task<InterpretationResult> InterpretAsync(
        InterpretationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsPlatformSupported)
        {
            var rule = await _fallback.InterpretAsync(request, cancellationToken);
            return rule with { Error = AppendNote(rule.Error, "当前平台暂不支持端侧 ONNX，已使用规则解读") };
        }

        var modelPath = ResolveModelPath();
        if (string.IsNullOrWhiteSpace(modelPath) || !Directory.Exists(modelPath))
        {
            var rule = await _fallback.InterpretAsync(request, cancellationToken);
            return rule with { Error = AppendNote(rule.Error, "未找到本地模型目录，已使用规则解读") };
        }

        EnsureEngine(modelPath);
        if (_engine is null)
        {
            var rule = await _fallback.InterpretAsync(request, cancellationToken);
            return rule with { Error = AppendNote(rule.Error, "本地引擎初始化失败，已使用规则解读") };
        }

        var question = PromptInputSanitizer.SanitizeUserText(request.Question);
        var focus = PromptInputSanitizer.SanitizeUserText(request.Focus);
        var prompt = request.FallbackPacket is not null
            ? ReadingPromptProtocol.BuildUserMessage(
                request.FallbackPacket with { Question = question, Focus = focus })
            : ReadingSummaries.BuildChatPrompt(
                request.Domain,
                question,
                focus,
                request.Chart ?? new { },
                request.RuleDigest);

        var system = request.FallbackPacket is not null
            ? ReadingPromptProtocol.BuildSystemPrompt(request.FallbackPacket)
            : $"你是谨慎的{request.Domain}解读助手。盘面由系统计算，勿改动已计算事实。用简体中文。";

        var fullPrompt = $"{system}\n\n{prompt}";
        var result = await _engine.GenerateAsync(
            fullPrompt,
            new GenerateOptions(MaxTokens: Math.Clamp(_settings.MaxTokens, 128, 1024)),
            cancellationToken);

        if (result.IsFallback || string.IsNullOrWhiteSpace(result.Text))
        {
            var rule = await _fallback.InterpretAsync(request, cancellationToken);
            return rule with
            {
                Error = AppendNote(rule.Error, result.FallbackReason ?? "本地模型生成失败，已降级规则解读")
            };
        }

        return new InterpretationResult(
            ReadingPromptProtocol.NormalizeOutput(result.Text),
            IsFallback: false,
            Error: null);
    }

    public Task<ConnectionTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPlatformSupported)
        {
            return Task.FromResult(new ConnectionTestResult(false, "当前平台不支持端侧 ONNX（iOS 待 Core AI / ORT GenAI）"));
        }

        var path = ResolveModelPath();
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return Task.FromResult(new ConnectionTestResult(false, "模型目录不存在，请先下载 Qwen3.5 INT4 GenAI 包"));
        }

        if (!File.Exists(Path.Combine(path, "genai_config.json")))
        {
            return Task.FromResult(new ConnectionTestResult(false, "模型目录缺少 genai_config.json"));
        }

        return Task.FromResult(new ConnectionTestResult(true, $"本地模型就绪：{path}"));
    }

    public async IAsyncEnumerable<string> StreamFollowUpAsync(
        IReadOnlyList<ChatTurn> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public void Dispose() => _engine?.Dispose();

    private string? ResolveModelPath()
    {
        if (!string.IsNullOrWhiteSpace(_settings.LocalOnnxModelPath))
        {
            return _settings.LocalOnnxModelPath;
        }

        // 约定：应用数据目录 /models/qwen3.5-*-genai 或仓库 models/
        var roots = new[]
        {
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
        };
        foreach (var root in roots)
        {
            foreach (var name in Qwen35ModelCatalog.CandidateDirectoryNames
                         .Append(Qwen35ModelCatalog.LegacyId)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var path = Path.Combine(root, "models", name);
                if (File.Exists(Path.Combine(path, "genai_config.json")))
                {
                    return path;
                }
            }
        }

        return null;
    }

    private void EnsureEngine(string modelPath)
    {
        if (_engine is not null)
        {
            return;
        }

        _engine = new OnnxGenAiEngine(modelPath, _logger);
    }

    private static string? AppendNote(string? existing, string note) =>
        string.IsNullOrWhiteSpace(existing) ? note : $"{existing}；{note}";
}
