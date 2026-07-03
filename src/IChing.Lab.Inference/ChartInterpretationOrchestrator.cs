using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Inference.Prompts;
using LabModels = IChing.Lab.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace IChing.Lab.Inference;

/// <summary>
/// 解读编排器：按 EngineId 选择引擎，编排单 pass 解读与塔罗英译中两 pass 流程。
/// 本类不包含任何 ONNX Model/Tokenizer 加载代码，所有模型相关逻辑均在引擎实现内。
/// </summary>
public sealed class ChartInterpretationOrchestrator
{
    private const string DefaultEngineId = "onnx-genai-qwen2.5-1.5b";
    private const string FallbackEngineId = "template-fallback";
    private readonly IReadOnlyDictionary<string, IInferenceEngine> _engines;
    private readonly ILogger<ChartInterpretationOrchestrator> _logger;

    public ChartInterpretationOrchestrator(
        IEnumerable<IInferenceEngine> engines,
        ILogger<ChartInterpretationOrchestrator> logger)
    {
        _engines = engines.ToDictionary(e => e.EngineId);
        _logger = logger;
    }

    /// <summary>默认 ONNX 引擎是否已加载模型（供原 IsModelLoaded 调用方使用）。</summary>
    public bool IsModelLoaded => SelectEngine(DefaultEngineId)?.IsReady ?? false;

    /// <summary>按 EngineId 选择引擎，未找到返回 null。</summary>
    public IInferenceEngine? SelectEngine(string engineId) =>
        _engines.TryGetValue(engineId, out var engine) ? engine : null;

    /// <summary>
    /// 单 pass 解读：构建 prompt → 调用默认引擎 → 失败时降级到模板兜底引擎。
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

        var gen = Generate(prompt, maxTokens, DefaultEngineId);
        if (gen.IsFallback)
        {
            // 主引擎降级：交给模板兜底引擎生成回退文本。
            var fallback = Generate(prompt, maxTokens, FallbackEngineId);
            return new InterpretResult(fallback.EngineId, fallback.Text, IsFallback: true);
        }

        return new InterpretResult(gen.EngineId, gen.Text, IsFallback: false);
    }

    /// <summary>
    /// 塔罗英译中两 pass：复用同一引擎，第一次生成英文初稿，第二次翻译为中文。
    /// </summary>
    public TarotInterpretResult InterpretTarotEnglishThenChinese(
        string englishPrompt,
        int englishMaxTokens = 512,
        int translateMaxTokens = 512)
    {
        var engineId = DefaultEngineId;

        var pass1 = Generate(englishPrompt, englishMaxTokens, engineId);
        if (pass1.IsFallback || string.IsNullOrWhiteSpace(pass1.Text))
        {
            return new TarotInterpretResult(engineId, pass1.Text, TextEn: null, pass1.IsFallback, pass1.FallbackReason);
        }

        var translatePrompt = TarotPromptBuilder.BuildTranslateToChinese(pass1.Text, ExtractTarotCardNames(englishPrompt));
        var pass2 = Generate(translatePrompt, translateMaxTokens, engineId);
        if (pass2.IsFallback || string.IsNullOrWhiteSpace(pass2.Text) || LooksLikeBadTranslation(pass2.Text))
        {
            return new TarotInterpretResult(
                engineId,
                pass1.Text,
                pass1.Text,
                IsFallback: true,
                pass2.FallbackReason ?? "translation failed quality gate");
        }

        return new TarotInterpretResult(
            engineId,
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
        var prompt = PromptFixtureLoader.BuildPrompt(fixture);
        var maxTokens = PromptFixtureLoader.GetMaxTokens(fixture);

        if (PromptFixtureLoader.NeedsTranslation(fixture))
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
