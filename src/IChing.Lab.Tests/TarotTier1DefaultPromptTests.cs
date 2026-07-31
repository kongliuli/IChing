using System.IO;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Core.Readings;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Prompts;
using Microsoft.Extensions.Logging.Abstractions;

namespace IChing.Lab.Tests;

/// <summary>
/// T6/T10：tarot-tier1-default 六段模板渲染、NeedsTranslationPass、meta 与 fixture 全链路。
/// </summary>
public class TarotTier1DefaultPromptTests : IDisposable
{
    private readonly TempDir _dir;

    public TarotTier1DefaultPromptTests() => _dir = new();
    public void Dispose() => _dir.Dispose();

    private static TarotPromptInput MakeInput(
        string? zodiac = null,
        int wordLimit = 400) =>
        new(
            SpreadTitle: "三牌 Celtic",
            Positions: new[]
            {
                new TarotPositionPrompt("过去", "Past", "The Tower", false, "突变"),
                new TarotPositionPrompt("现在", "Present", "The Star", true, "希望"),
                new TarotPositionPrompt("未来", "Future", "Eight of Pentacles", false, "精进")
            },
            WordLimit: wordLimit,
            Zodiac: zodiac);

    private static PromptContext MakeCtx(TarotPromptInput input, string? followUp = null) =>
        new(
            Chart: input,
            RuleDigest: new { Major = "凯尔特十字" },
            Question: "我的事业前景如何？",
            Focus: "综合",
            MaxTokens: 800,
            FollowUp: followUp);

    private TemplatePromptBuilder CreateBuilder(string templateId = "tarot-tier1-default")
    {
        var registry = new PromptTemplateRegistry(_dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        return new TemplatePromptBuilder(registry, "tarot", 1, templateId);
    }

    [Fact]
    public void Zodiac_Rendered_WhenProvided()
    {
        var input = MakeInput(zodiac: "天蝎座");
        var ctx = MakeCtx(input);
        var result = CreateBuilder().Build(ctx);

        Assert.Contains("【星座】占卜者星座：天蝎座", result.PromptText);
    }

    [Fact]
    public void Zodiac_Omitted_WhenNull()
    {
        var input = MakeInput(zodiac: null);
        var ctx = MakeCtx(input);
        var result = CreateBuilder().Build(ctx);

        Assert.DoesNotContain("【星座】", result.PromptText);
    }

    [Fact]
    public void FollowUp_Rendered_WhenProvided()
    {
        var input = MakeInput();
        var ctx = MakeCtx(input, followUp: "再问事业");
        var result = CreateBuilder().Build(ctx);

        Assert.Contains("【追问】再问事业", result.PromptText);
    }

    [Fact]
    public void FollowUp_Omitted_WhenNull()
    {
        var input = MakeInput();
        var ctx = MakeCtx(input, followUp: null);
        var result = CreateBuilder().Build(ctx);

        Assert.DoesNotContain("【追问】", result.PromptText);
    }

    [Fact]
    public void SixSection_ZodiacAndFollowUp_RenderedTogether()
    {
        var input = MakeInput(zodiac: "天蝎座");
        var ctx = MakeCtx(input, followUp: "再补充一下时间线");
        var result = CreateBuilder().Build(ctx);

        Assert.Contains("【牌阵】", result.PromptText);
        Assert.Contains("【牌面】", result.PromptText);
        Assert.Contains("【释义】", result.PromptText);
        Assert.Contains("【星座】占卜者星座：天蝎座", result.PromptText);
        Assert.Contains("【提问】", result.PromptText);
        Assert.Contains("【追问】再补充一下时间线", result.PromptText);
    }

    [Fact]
    public void SixSection_ZodiacAndFollowUp_BothOmitted_WhenNull()
    {
        var input = MakeInput(zodiac: null);
        var ctx = MakeCtx(input, followUp: null);
        var result = CreateBuilder().Build(ctx);

        Assert.DoesNotContain("【星座】", result.PromptText);
        Assert.DoesNotContain("【追问】", result.PromptText);
        Assert.Contains("【牌阵】", result.PromptText);
        Assert.Contains("【提问】", result.PromptText);
    }

    [Fact]
    public void NeedsTranslationPass_Default_IsFalse()
    {
        var registry = new PromptTemplateRegistry(_dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "tarot", 1, "tarot-tier1-default");
        var input = MakeInput();
        var result = builder.Build(MakeCtx(input));

        Assert.False(result.NeedsTranslationPass);
    }

    [Fact]
    public void NeedsTranslationPass_En_IsTrue_CompatibilitySentinel()
    {
        var registry = new PromptTemplateRegistry(_dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "tarot", 1, "tarot-tier1-en");
        var input = MakeInput();
        var result = builder.Build(MakeCtx(input));

        Assert.True(result.NeedsTranslationPass);
    }

    [Fact]
    public void GetMeta_TarotTier1Default_ReturnsExpectedFields()
    {
        var repoPrompts = Path.Combine("..", "..", "..", "..", "prompts");
        if (!Directory.Exists(repoPrompts))
        {
            return;
        }

        using var registry = new PromptTemplateRegistry(repoPrompts, NullLogger<PromptTemplateRegistry>.Instance);
        var meta = registry.GetMeta("tarot-tier1-default");

        Assert.NotNull(meta);
        Assert.Equal("tarot-tier1-default", meta!.TemplateId);
        Assert.False(meta.NeedsTranslationPass);
        Assert.Equal(400, meta.WordLimit);
        Assert.Equal(800, meta.MaxTokens);
        Assert.Equal(2, meta.OutputSections.Count);
        Assert.Equal("overview", meta.OutputSections[0].Key);
        Assert.Equal("整体能量", meta.OutputSections[0].Title);
        Assert.Equal("advice", meta.OutputSections[1].Key);
        Assert.Equal("行动建议", meta.OutputSections[1].Title);
    }

    [Fact]
    public void GetMeta_Nonexistent_ReturnsNull()
    {
        using var registry = new PromptTemplateRegistry(_dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        Assert.Null(registry.GetMeta("nonexistent"));
    }

    [Fact]
    public void Fixture_ResolveTemplateId_LanguageDefault_MapsToTarotTier1Default()
    {
        var fixturePath = Path.Combine(
            "..", "..", "..", "..", "docs", "active", "prompts", "fixtures", "tarot-tier1-default.json");
        if (!File.Exists(fixturePath))
        {
            return;
        }

        var fixture = PromptFixtureLoader.Load(fixturePath);
        Assert.Equal("tarot-tier1-default", fixture.Id);
        Assert.Equal("default", fixture.Language);
        Assert.Equal("tarot-tier1-default", PromptFixtureLoader.ResolveTemplateId(fixture));
        Assert.Equal("今年是否适合换工作?", fixture.Raw.GetProperty("question").GetString());
        Assert.False(PromptFixtureLoader.NeedsTranslation(fixture));
    }

    [Fact]
    public void Fixture_BuildPrompt_ViaDefaultBuilder_RendersChineseTemplate()
    {
        var fixturePath = Path.Combine(
            "..", "..", "..", "..", "docs", "active", "prompts", "fixtures", "tarot-tier1-default.json");
        if (!File.Exists(fixturePath))
        {
            return;
        }

        var fixture = PromptFixtureLoader.Load(fixturePath);
        var templateId = PromptFixtureLoader.ResolveTemplateId(fixture);
        var registry = new PromptTemplateRegistry(_dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "tarot", fixture.Tier, templateId);
        var result = PromptFixtureLoader.BuildPrompt(fixture, builder);

        Assert.Equal("tarot-tier1-default", templateId);
        Assert.False(result.NeedsTranslationPass);
        Assert.Contains("你是塔罗解读助手", result.PromptText);
        Assert.Contains("【提问】今年是否适合换工作?", result.PromptText);
        Assert.DoesNotContain("【星座】", result.PromptText);
        Assert.DoesNotContain("【追问】", result.PromptText);
    }

    [Fact]
    public void EmbeddedFallback_WhenDiskMissing()
    {
        var input = MakeInput(zodiac: "双鱼座");
        var ctx = MakeCtx(input);
        var result = CreateBuilder().Build(ctx);

        Assert.Contains("你是塔罗解读助手", result.PromptText);
        Assert.Contains("占卜者星座：双鱼座", result.PromptText);
    }

    [Fact]
    public void Append_TarotTier1Default_IncludesJsonContract()
    {
        var result = ReadingJsonOutputContract.Append("tarot", "test", "tarot-tier1-default");
        Assert.Contains("请仅返回一个合法 JSON 对象", result);
    }

    [Fact]
    public void OutputSections_MatchMeta_NoDrift()
    {
        var repoPrompts = Path.Combine("..", "..", "..", "..", "prompts");
        if (!Directory.Exists(repoPrompts))
        {
            return;
        }

        using var registry = new PromptTemplateRegistry(repoPrompts, NullLogger<PromptTemplateRegistry>.Instance);
        var metaKeys = registry.GetMeta("tarot-tier1-default")?.OutputSections.Select(s => s.Key).ToArray();
        Assert.NotNull(metaKeys);

        var hardcodedKeys = ReadingPromptTemplateManager.Get("tarot", "initial")
            .OutputSections.Select(s => s.Key)
            .ToArray();

        Assert.Equal(metaKeys, hardcodedKeys);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iching-tarot-zh-test-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch { /* ignore */ }
        }
    }
}

