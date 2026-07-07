using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Inference.Prompts;
using LabModels = IChing.Lab.Abstractions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IChing.Lab.Inference;

/// <summary>
/// 解读编排器：按 <c>plugins:fallbackChain</c> 配置顺序尝试各引擎，任一成功即返回；
/// 全部失败时使用 <see cref="TemplateFallbackEngine"/> 兜底，编排单 pass 解读与塔罗英译中两 pass 流程。
/// 本类不包含任何 ONNX Model/Tokenizer 加载代码，所有模型相关逻辑均在引擎实现内。
/// </summary>
public sealed class ChartInterpretationOrchestrator
{
    private const string DefaultEngineId = "onnx-genai-qwen2.5-1.5b";
    private const string FallbackEngineId = "template-fallback";
    private const string TarotTranslateTemplateId = "tarot-translate-to-zh";
    /// <summary>配置未声明降级链时使用的默认链：默认引擎 → 模板兜底。</summary>
    private static readonly string[] DefaultFallbackChain = { DefaultEngineId, FallbackEngineId };
    private readonly IReadOnlyDictionary<string, IInferenceEngine> _engines;
    private readonly IReadOnlyDictionary<string, IPromptBuilder> _promptBuilders;
    private readonly ILogger<ChartInterpretationOrchestrator> _logger;
    private readonly IConfiguration _configuration;
    // 排盘引擎集合（按 Domain:EngineId 索引），供算法感知的模板选择解析 EngineMetadata / ModuleFocus。
    // bazi 与 calendar 共用 lunar-csharp-1.6.8，单用 EngineId 会冲突。
    // 默认空集合：旧调用方/测试不注入排盘引擎时，ResolveEngineMetadata 返回 null，行为与改造前一致。
    private readonly IReadOnlyDictionary<string, IChartEngine> _chartEngines;

    public ChartInterpretationOrchestrator(
        IEnumerable<IInferenceEngine> engines,
        IEnumerable<IPromptBuilder> promptBuilders,
        IConfiguration configuration,
        ILogger<ChartInterpretationOrchestrator> logger,
        IEnumerable<IChartEngine>? chartEngines = null)
    {
        _engines = engines
            .GroupBy(e => e.EngineId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        // 按 TemplateId 索引 PromptBuilder，供 RunFixture / 翻译 pass 按 domain+tier+templateId 选取。
        _promptBuilders = promptBuilders.ToDictionary(b => b.TemplateId);
        _configuration = configuration;
        _logger = logger;
        _chartEngines = (chartEngines ?? Enumerable.Empty<IChartEngine>())
            .ToDictionary(e => ChartEngineKey(e.Domain, e.EngineId));
    }

    /// <summary>排盘引擎字典键：Domain 与 EngineId 组合，避免跨域共用同一 EngineId 时冲突。</summary>
    internal static string ChartEngineKey(string domain, string engineId) => $"{domain}:{engineId}";

    /// <summary>
    /// 按 domain + EngineId 解析排盘引擎元数据；未注册返回 null。
    /// 供调用方在构造 PromptContext 时填充 <see cref="PromptContext.Engine"/> 字段，驱动算法感知的模板选择。
    /// </summary>
    public EngineMetadata? ResolveEngineMetadata(string domain, string engineId) =>
        _chartEngines.TryGetValue(ChartEngineKey(domain, engineId), out var engine) ? engine.Metadata : null;

    /// <summary>
    /// 按 domain + EngineId 解析排盘引擎的模块面向列表；未注册返回空数组。
    /// 供调用方填充 <see cref="PromptContext.ModuleFocuses"/>，驱动多模块组合 prompt 拼装。
    /// </summary>
    public IReadOnlyList<string> ResolveModuleFocuses(string domain, string engineId) =>
        _chartEngines.TryGetValue(ChartEngineKey(domain, engineId), out var engine)
            ? engine.Metadata.ModuleFocus
            : Array.Empty<string>();

    /// <summary>按 templateId 选取已注册的 IPromptBuilder；未注册返回 null。</summary>
    public IPromptBuilder? SelectPromptBuilder(string templateId) =>
        _promptBuilders.TryGetValue(templateId, out var builder) ? builder : null;

    /// <summary>默认 ONNX 引擎是否已加载模型（供原 IsModelLoaded 调用方使用）。</summary>
    public bool IsModelLoaded => SelectEngine(DefaultEngineId)?.IsReady ?? false;

    /// <summary>按 EngineId 选择引擎，未找到返回 null。</summary>
    public IInferenceEngine? SelectEngine(string engineId) =>
        _engines.TryGetValue(engineId, out var engine) ? engine : null;

    /// <summary>
    /// 按降级链生成：依次尝试 <c>plugins:fallbackChain</c> 中每个引擎，任一成功（IsFallback=false）即返回；
    /// 引擎未注册 / 未就绪 / 返回降级结果 / 抛异常均记录并继续下一个；全部失败时强制调用
    /// <see cref="TemplateFallbackEngine"/> 兜底，<see cref="GenerationResult.IsFallback"/>=true，
    /// <see cref="GenerationResult.FallbackReason"/> 描述所有失败引擎，绝不向调用方抛异常。
    /// </summary>
    public async Task<LabModels.GenerationResult> GenerateWithFallbackAsync(
        string prompt, GenerateOptions options, CancellationToken ct)
    {
        var chain = ResolveFallbackChain();
        var failures = new List<string>(chain.Length);

        foreach (var engineId in chain)
        {
            if (string.IsNullOrWhiteSpace(engineId))
            {
                continue;
            }

            if (!_engines.TryGetValue(engineId, out var engine))
            {
                failures.Add($"{engineId}(未注册)");
                _logger.LogWarning("降级链：引擎 {EngineId} 未注册，跳过。", engineId);
                continue;
            }

            if (!engine.IsReady)
            {
                failures.Add($"{engineId}(未就绪)");
                _logger.LogInformation("降级链：引擎 {EngineId} 未就绪，尝试下一个。", engineId);
                continue;
            }

            LabModels.GenerationResult result;
            try
            {
                _logger.LogInformation("降级链：尝试引擎 {EngineId}。", engineId);
                result = await engine.GenerateAsync(prompt, options, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                // 调用方主动取消（ct.IsCancellationRequested）时向上传播；其余异常记录后继续降级。
                failures.Add($"{engineId}(异常:{ex.Message})");
                _logger.LogWarning(ex, "降级链：引擎 {EngineId} 抛异常，尝试下一个。", engineId);
                continue;
            }

            if (!result.IsFallback)
            {
                _logger.LogInformation("降级链：引擎 {EngineId} 成功生成（耗时 {ElapsedMs}ms）。", engineId, result.ElapsedMs);
                return result;
            }

            failures.Add($"{engineId}({result.FallbackReason ?? "内部降级"})");
            _logger.LogWarning("降级链：引擎 {EngineId} 返回降级结果：{Reason}", engineId, result.FallbackReason);
        }

        // 全部失败：强制使用 template-fallback 兜底，不抛异常给调用方。
        var reason = $"所有引擎均失败：{string.Join("; ", failures)}";
        _logger.LogWarning("降级链：{Reason}，使用 template-fallback 兜底。", reason);

        if (_engines.TryGetValue(FallbackEngineId, out var fallbackEngine))
        {
            try
            {
                var fbResult = await fallbackEngine.GenerateAsync(prompt, options, ct).ConfigureAwait(false);
                return new LabModels.GenerationResult(
                    fbResult.EngineId,
                    fbResult.Text,
                    IsFallback: true,
                    FallbackReason: reason,
                    fbResult.ElapsedMs);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "template-fallback 引擎也抛异常，返回空兜底结果。");
            }
        }

        // 极端情况：template-fallback 也未注册或抛异常，返回空兜底结果，保证不抛异常。
        return new LabModels.GenerationResult(
            FallbackEngineId,
            Text: string.Empty,
            IsFallback: true,
            FallbackReason: reason,
            ElapsedMs: 0);
    }

    /// <summary>
    /// 解析降级链配置：读取 <c>plugins:fallbackChain</c> 数组；为空或未配置时返回默认链
    /// （<see cref="DefaultEngineId"/> → <see cref="FallbackEngineId"/>）。
    /// </summary>
    private string[] ResolveFallbackChain()
    {
        var section = _configuration.GetSection("plugins:fallbackChain");
        var chain = section.GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToArray();
        return chain.Length > 0 ? chain : DefaultFallbackChain;
    }

    /// <summary>
    /// 单 pass 解读：构建 prompt → 调用降级链生成，按 plugins:fallbackChain 顺序自动尝试各引擎。
    /// </summary>
    public InterpretResult Interpret(object chartJson, string? focus = null, int maxTokens = 256)
    {
        var chart = JsonSerializer.Serialize(CompactForPrompt(chartJson));
        var prompt = QwenChatTemplate.Wrap(
            "你是命理解读助手。以下命盘由系统计算，请勿修改干支数据。",
            $"""
            请用中文写一段不超过200字的解读。
            关注角度：{focus ?? "综合"}
            命盘JSON：
            {chart}
            """);

        var gen = GenerateWithFallbackAsync(prompt, new GenerateOptions(MaxTokens: maxTokens), CancellationToken.None)
            .GetAwaiter().GetResult();
        return new InterpretResult(gen.EngineId, gen.Text, gen.IsFallback);
    }

    /// <summary>
    /// 塔罗英译中两 pass：第一 pass 生成英文初稿、第二 pass 翻译为中文，两 pass 均走降级链，
    /// 自动按 plugins:fallbackChain 顺序尝试各引擎。
    /// </summary>
    public TarotInterpretResult InterpretTarotEnglishThenChinese(
        string englishPrompt,
        int englishMaxTokens = 512,
        int translateMaxTokens = 512)
    {
        // 第一 pass：英文初稿，走降级链。
        var pass1 = GenerateWithFallbackAsync(
                englishPrompt, new GenerateOptions(MaxTokens: englishMaxTokens), CancellationToken.None)
            .GetAwaiter().GetResult();

        if (pass1.IsFallback || string.IsNullOrWhiteSpace(pass1.Text))
        {
            return new TarotInterpretResult(
                pass1.EngineId, pass1.Text, TextEn: null, pass1.IsFallback, pass1.FallbackReason);
        }

        // 翻译 pass 改用 IPromptBuilder（tarot-translate-to-zh 模板）构建；builder 未注册时降级返回英文初稿。
        var translateBuilder = SelectPromptBuilder(TarotTranslateTemplateId);
        if (translateBuilder is null)
        {
            return new TarotInterpretResult(
                pass1.EngineId,
                pass1.Text,
                pass1.Text,
                IsFallback: true,
                "translate prompt builder not registered");
        }

        var cardNames = ExtractTarotCardNames(englishPrompt);
        var translateCtx = new PromptContext(
            Chart: pass1.Text,
            RuleDigest: cardNames,
            Question: null,
            Focus: null,
            MaxTokens: translateMaxTokens);
        var translatePrompt = translateBuilder.Build(translateCtx).PromptText;

        // 第二 pass：中译，同样走降级链，复用同一组引擎。
        var pass2 = GenerateWithFallbackAsync(
                translatePrompt, new GenerateOptions(MaxTokens: translateMaxTokens), CancellationToken.None)
            .GetAwaiter().GetResult();
        if (pass2.IsFallback || string.IsNullOrWhiteSpace(pass2.Text) || LooksLikeBadTranslation(pass2.Text))
        {
            return new TarotInterpretResult(
                pass2.EngineId,
                pass1.Text,
                pass1.Text,
                IsFallback: true,
                pass2.FallbackReason ?? "translation failed quality gate");
        }

        return new TarotInterpretResult(
            pass2.EngineId,
            CleanTranslationPrefix(pass2.Text),
            pass1.Text,
            IsFallback: false,
            FallbackReason: null);
    }

    /// <summary>
    /// 运行 PromptTest fixture：塔罗走两 pass，其余走单 pass。
    /// </summary>
    public InterpretResult RunFixture(PromptFixture fixture)
    {
        // 通过 IPromptBuilder（按 fixture 推导的 templateId）构建 prompt；builder 未注册时降级。
        var templateId = PromptFixtureLoader.ResolveTemplateId(fixture);
        var builder = SelectPromptBuilder(templateId);
        if (builder is null)
        {
            return new InterpretResult(
                FallbackEngineId,
                $"[prompt builder not registered: {templateId}]",
                IsFallback: true);
        }

        var promptResult = PromptFixtureLoader.BuildPrompt(fixture, builder);
        var prompt = promptResult.PromptText;
        var maxTokens = PromptFixtureLoader.GetMaxTokens(fixture);

        if (promptResult.NeedsTranslationPass)
        {
            var tarot = InterpretTarotEnglishThenChinese(
                prompt,
                maxTokens,
                PromptFixtureLoader.GetTranslateMaxTokens(fixture));

            return new InterpretResult(
                tarot.Engine,
                tarot.TextZh,
                tarot.IsFallback,
                tarot.TextEn);
        }

        var gen = Generate(prompt, maxTokens, DefaultEngineId);
        return new InterpretResult(gen.EngineId, gen.Text, gen.IsFallback);
    }

    /// <summary>
    /// 单 pass 生成（LabController / PromptTest 直接调用）。
    /// </summary>
    public LabModels.GenerationResult Generate(string prompt, int maxTokens = 512, string? engineId = null)
    {
        var resolvedId = engineId ?? DefaultEngineId;
        var engine = SelectEngine(resolvedId);
        if (engine is null)
        {
            return new LabModels.GenerationResult(
                resolvedId,
                Text: string.Empty,
                IsFallback: true,
                FallbackReason: $"engine not found: {resolvedId}",
                ElapsedMs: 0);
        }

        var options = new LabModels.GenerateOptions(MaxTokens: maxTokens);
        return engine.GenerateAsync(prompt, options, CancellationToken.None).GetAwaiter().GetResult();
    }

    // ---- 以下为 prompt 压缩与塔罗翻译辅助方法（编排逻辑，不含模型加载） ----

    private static object CompactForPrompt(object chartJson) =>
        chartJson is BaziChart chart ? CompactBaziForPrompt(chart) : chartJson;

    private static object CompactBaziForPrompt(BaziChart chart) => new
    {
        chart.Engine,
        chart.WallClock,
        chart.TrueSolarTime,
        chart.Solar,
        chart.Lunar,
        chart.DayMaster,
        pillars = new
        {
            year = CompactPillar(chart.YearPillar),
            month = CompactPillar(chart.MonthPillar),
            day = CompactPillar(chart.DayPillar),
            hour = CompactPillar(chart.HourPillar)
        },
        chart.WuXingSummary,
        chart.Yun,
        daYun = chart.DaYun?.Take(5),
        flowYear = chart.FlowYear is null ? null : new
        {
            chart.FlowYear.Year,
            chart.FlowYear.GanZhi,
            chart.FlowYear.Age,
            chart.FlowYear.DaYunGanZhi,
            selectedMonth = chart.FlowYear.SelectedMonth is null ? null : new
            {
                chart.FlowYear.SelectedMonth.Index,
                chart.FlowYear.SelectedMonth.MonthInChinese,
                chart.FlowYear.SelectedMonth.GanZhi,
                chart.FlowYear.SelectedMonth.JieQiStart,
                chart.FlowYear.SelectedMonth.StartSolar,
                chart.FlowYear.SelectedMonth.JieQiEnd,
                chart.FlowYear.SelectedMonth.EndSolar
            },
            chart.FlowYear.SelectedDay
        },
        yongShen = new
        {
            chart.YongShen.Strength,
            geJu = chart.YongShen.GeJu.Pattern,
            geJuBreak = chart.YongShen.GeJu.Break?.Summary,
            chart.YongShen.PrimaryYongShen,
            chart.YongShen.SecondaryYongShen,
            chart.YongShen.FavoredElements,
            chart.YongShen.Summary
        }
    };

    private static object CompactPillar(BaziPillar p) => new
    {
        p.GanZhi,
        p.Gan,
        p.Zhi,
        p.WuXing,
        p.NaYin,
        p.ShiShenGan,
        p.HideGan
    };

    private static IReadOnlyList<string> ExtractTarotCardNames(string prompt) =>
        prompt.Split('\n')
            .Select(ExtractCardNameFromPositionLine)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct()
            .ToList();

    private static string? ExtractCardNameFromPositionLine(string line)
    {
        var close = line.IndexOf("] ", StringComparison.Ordinal);
        var orient = line.IndexOf(" (", StringComparison.Ordinal);
        if (close < 0 || orient <= close)
        {
            return null;
        }

        return line[(close + 2)..orient].Trim();
    }

    private static string CleanTranslationPrefix(string text)
    {
        var trimmed = text.Trim();
        return trimmed.StartsWith("中文：", StringComparison.Ordinal)
            ? trimmed["中文：".Length..].TrimStart()
            : trimmed;
    }

    private static bool LooksLikeBadTranslation(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Contains("English:", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("Protected card names", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("Card name glossary", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("守护的牌名", StringComparison.Ordinal);
    }
}
