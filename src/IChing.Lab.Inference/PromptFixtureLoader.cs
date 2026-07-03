using System.Text.Json;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Inference.Prompts;

namespace IChing.Lab.Inference;

public static class PromptFixtureLoader
{
    public static PromptFixture Load(string path)
    {
        var json = File.ReadAllText(path);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new PromptFixture(
            Id: root.GetProperty("id").GetString()!,
            Domain: root.GetProperty("domain").GetString()!,
            Tier: root.GetProperty("tier").GetInt32(),
            Language: root.TryGetProperty("language", out var lang) ? lang.GetString() : "zh",
            Raw: root,
            SourcePath: path);
    }

    /// <summary>
    /// 通过 <see cref="IPromptBuilder"/> 构建.fixture 的 Prompt，返回带 NeedsTranslationPass 元信息的结果。
    /// </summary>
    public static PromptBuildResult BuildPrompt(PromptFixture fixture, IPromptBuilder builder)
    {
        var root = fixture.Raw;
        PromptContext ctx = fixture.Domain switch
        {
            "bazi" => new PromptContext(
                Chart: JsonSerializer.Deserialize<object>(root.GetProperty("chart").GetRawText())!,
                RuleDigest: JsonSerializer.Deserialize<object>(root.GetProperty("ruleDigest").GetRawText())!,
                Question: null,
                Focus: root.TryGetProperty("focus", out var bf) ? bf.GetString() : null,
                MaxTokens: GetMaxTokens(fixture)),

            "liuyao" => new PromptContext(
                Chart: JsonSerializer.Deserialize<object>(root.GetProperty("chart").GetRawText())!,
                RuleDigest: JsonSerializer.Deserialize<object>(root.GetProperty("ruleDigest").GetRawText())!,
                Question: root.GetProperty("question").GetString(),
                Focus: root.TryGetProperty("focus", out var lf) ? lf.GetString() : null,
                MaxTokens: GetMaxTokens(fixture)),

            "tarot" => new PromptContext(
                Chart: BuildTarotInput(root),
                RuleDigest: JsonSerializer.Deserialize<object>(root.GetProperty("ruleDigest").GetRawText())!,
                Question: root.GetProperty("question").GetString(),
                Focus: null,
                MaxTokens: GetMaxTokens(fixture)),

            _ => throw new InvalidOperationException($"unknown domain: {fixture.Domain}")
        };

        return builder.Build(ctx);
    }

    /// <summary>由 fixture 推导 templateId：tarot 用 language 作为 variant，其余用 default。</summary>
    public static string ResolveTemplateId(PromptFixture fixture)
    {
        var variant = fixture.Domain == "tarot"
            ? (fixture.Language ?? "en")
            : "default";
        return $"{fixture.Domain}-tier{fixture.Tier}-{variant}";
    }

    private static TarotPromptInput BuildTarotInput(JsonElement root)
    {
        var positions = root.GetProperty("positions").EnumerateArray()
            .Select(p => new TarotPositionPrompt(
                p.GetProperty("positionTitle").GetString()!,
                p.GetProperty("positionContext").GetString()!,
                p.GetProperty("cardName").GetString()!,
                p.GetProperty("reversed").GetBoolean(),
                p.GetProperty("meaningEn").GetString()!))
            .ToList();

        var spreadTitle = root.GetProperty("spreadTitle").GetString()!;
        var wordLimit = root.TryGetProperty("wordLimit", out var wl) ? wl.GetInt32() : 280;
        return new TarotPromptInput(spreadTitle, positions, wordLimit);
    }

#pragma warning disable CS0618 // 保留向下兼容的静态构建路径，仅供已 [Obsolete] 的 ChartInterpretationService 使用
    [Obsolete("改用 BuildPrompt(fixture, IPromptBuilder)")]
    public static string BuildPrompt(PromptFixture fixture)
    {
        var root = fixture.Raw;
        return fixture.Domain switch
        {
            "liuyao" => LiuyaoPromptBuilder.BuildTier1(
                root.GetProperty("question").GetString()!,
                root.TryGetProperty("focus", out var f) ? f.GetString() : null,
                JsonSerializer.Deserialize<object>(root.GetProperty("ruleDigest").GetRawText())!,
                JsonSerializer.Deserialize<object>(root.GetProperty("chart").GetRawText())!),

            "bazi" => BaziPromptBuilder.BuildTier1(
                JsonSerializer.Deserialize<object>(root.GetProperty("chart").GetRawText())!,
                JsonSerializer.Deserialize<object>(root.GetProperty("ruleDigest").GetRawText())!,
                root.TryGetProperty("focus", out var bf) ? bf.GetString() : null),

            "tarot" => BuildTarotPrompt(root),

            _ => throw new InvalidOperationException($"unknown domain: {fixture.Domain}")
        };
    }

    private static string BuildTarotPrompt(JsonElement root)
    {
        var positions = root.GetProperty("positions").EnumerateArray()
            .Select(p => new TarotPositionPrompt(
                p.GetProperty("positionTitle").GetString()!,
                p.GetProperty("positionContext").GetString()!,
                p.GetProperty("cardName").GetString()!,
                p.GetProperty("reversed").GetBoolean(),
                p.GetProperty("meaningEn").GetString()!))
            .ToList();

        return TarotPromptBuilder.BuildEnglishTier1(
            root.GetProperty("question").GetString()!,
            root.GetProperty("spreadTitle").GetString()!,
            JsonSerializer.Deserialize<object>(root.GetProperty("ruleDigest").GetRawText())!,
            positions,
            root.TryGetProperty("wordLimit", out var wl) ? wl.GetInt32() : 280);
    }
#pragma warning restore CS0618

    public static int GetMaxTokens(PromptFixture fixture)
    {
        var root = fixture.Raw;
        return root.TryGetProperty("maxTokens", out var mt) ? mt.GetInt32() : 512;
    }

    public static int GetTranslateMaxTokens(PromptFixture fixture)
    {
        var root = fixture.Raw;
        return root.TryGetProperty("translateMaxTokens", out var mt) ? mt.GetInt32() : 512;
    }

    public static bool NeedsTranslation(PromptFixture fixture) =>
        fixture.Domain == "tarot" && fixture.Language == "en";

    public static IReadOnlyList<string> GetTarotCardNames(PromptFixture fixture)
    {
        if (fixture.Domain != "tarot" || !fixture.Raw.TryGetProperty("positions", out var positions))
        {
            return [];
        }

        return positions.EnumerateArray()
            .Select(p => p.GetProperty("cardName").GetString())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct()
            .ToList();
    }
}

public record PromptFixture(
    string Id,
    string Domain,
    int Tier,
    string? Language,
    JsonElement Raw,
    string SourcePath);
