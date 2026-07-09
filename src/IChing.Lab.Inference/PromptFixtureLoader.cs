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
