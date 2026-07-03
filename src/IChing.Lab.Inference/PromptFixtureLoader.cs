using System.Text.Json;
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
