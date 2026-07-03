using System.Diagnostics;
using System.Text;
using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Inference.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace IChing.Lab.Inference;

[Obsolete("Use ChartInterpretationOrchestrator + IInferenceEngine")]
public sealed class ChartInterpretationService : IDisposable
{
    private const int PromptTokenBuffer = 2048;
    private const int ContextLimit = 32768;
    private readonly string _modelPath;
    private readonly ILogger<ChartInterpretationService> _logger;
    private Model? _model;
    private Tokenizer? _tokenizer;
    private readonly object _gate = new();

    public ChartInterpretationService(string modelPath, ILogger<ChartInterpretationService> logger)
    {
        _modelPath = modelPath;
        _logger = logger;
    }

    public bool IsModelLoaded => _model is not null;

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

        var gen = Generate(prompt, maxTokens, "qwen-legacy-zh");
        if (gen.IsFallback)
        {
            return TemplateFallback(chartJson, focus, gen.FallbackReason ?? "model not loaded");
        }

        return new InterpretResult(gen.Engine, gen.Text, false);
    }

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

    public GenerationResult Generate(string prompt, int maxTokens = 512, string engine = "qwen2.5-1.5b-onnx-genai")
    {
        EnsureModel();
        if (_model is null || _tokenizer is null)
        {
            return new GenerationResult(engine, "", true, "model not loaded", 0);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var text = RunGeneration(prompt, maxTokens);
            return new GenerationResult(engine, text, false, null, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ONNX inference failed");
            return new GenerationResult(engine, "", true, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public TarotInterpretResult InterpretTarotEnglishThenChinese(
        string englishPrompt,
        int englishMaxTokens = 512,
        int translateMaxTokens = 512)
    {
        const string engine = "qwen2.5-1.5b-onnx-genai";

        var pass1 = Generate(englishPrompt, englishMaxTokens, engine);
        if (pass1.IsFallback || string.IsNullOrWhiteSpace(pass1.Text))
        {
            return new TarotInterpretResult(engine, pass1.Text, null, pass1.IsFallback, pass1.FallbackReason);
        }

        var translatePrompt = TarotPromptBuilder.BuildTranslateToChinese(pass1.Text, ExtractTarotCardNames(englishPrompt));
        var pass2 = Generate(translatePrompt, translateMaxTokens, engine);
        if (pass2.IsFallback || string.IsNullOrWhiteSpace(pass2.Text) || LooksLikeBadTranslation(pass2.Text))
        {
            return new TarotInterpretResult(engine, pass1.Text, pass1.Text, true, pass2.FallbackReason ?? "translation failed quality gate");
        }

        return new TarotInterpretResult(engine, CleanTranslationPrefix(pass2.Text), pass1.Text, false, null);
    }

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

        var gen = Generate(prompt, maxTokens);
        return new InterpretResult(gen.Engine, gen.Text, gen.IsFallback);
    }

    private string RunGeneration(string prompt, int maxTokens)
    {
        var sequences = _tokenizer!.Encode(prompt);
        using var parameters = new GeneratorParams(_model!);
        parameters.SetSearchOption("max_length", Math.Min(maxTokens + PromptTokenBuffer, ContextLimit));

        using var generator = new Generator(_model!, parameters);
        generator.AppendTokenSequences(sequences);

        using var stream = _tokenizer.CreateStream();
        var output = new StringBuilder();
        while (!generator.IsDone())
        {
            generator.GenerateNextToken();
            output.Append(stream.Decode(generator.GetSequence(0)[^1]));
        }

        return output.ToString().Trim();
    }

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

    private static InterpretResult TemplateFallback(object chartJson, string? focus, string reason)
    {
        var hint = reason.Contains("exceeds max length", StringComparison.OrdinalIgnoreCase)
            ? "Model is loaded, but this prompt exceeded the context window; reduce flow-year/month/day detail and retry."
            : "ONNX model is not ready; confirm model files exist and retry.";
        return new InterpretResult("template-fallback", $"[template fallback · {focus ?? "general"}] {hint} ({reason})", true);
    }

    private void EnsureModel()
    {
        if (_model is not null)
        {
            return;
        }

        lock (_gate)
        {
            if (_model is not null)
            {
                return;
            }

            if (!Directory.Exists(_modelPath))
            {
                _logger.LogWarning("ONNX model path not found: {Path}", _modelPath);
                return;
            }

            if (!File.Exists(Path.Combine(_modelPath, "genai_config.json")))
            {
                _logger.LogWarning("genai_config.json missing under {Path}", _modelPath);
                return;
            }

            _model = new Model(_modelPath);
            _tokenizer = new Tokenizer(_model);
            _logger.LogInformation("Loaded ONNX GenAI model from {Path}", _modelPath);
        }
    }

    public void Dispose()
    {
        _tokenizer?.Dispose();
        _model?.Dispose();
    }
}

public record GenerationResult(
    string Engine,
    string Text,
    bool IsFallback,
    string? FallbackReason,
    long ElapsedMs);

public record InterpretResult(string Engine, string Text, bool IsFallback, string? TextEn = null);

public record TarotInterpretResult(
    string Engine,
    string TextZh,
    string? TextEn,
    bool IsFallback,
    string? FallbackReason);
