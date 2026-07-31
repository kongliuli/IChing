using System.IO;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Core.Readings;
using IChing.Lab.Inference.Prompts;
using Microsoft.Extensions.Logging.Abstractions;

namespace IChing.Lab.Tests;

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

    private TemplatePromptBuilder CreateBuilder()
    {
        var registry = new PromptTemplateRegistry(_dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        return new TemplatePromptBuilder(registry, "tarot", 1, "tarot-tier1-default");
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
    public void NeedsTranslationPass_IsFalse()
    {
        var registry = new PromptTemplateRegistry(_dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "tarot", 1, "tarot-tier1-default");
        var input = MakeInput();
        var result = builder.Build(MakeCtx(input));

        Assert.False(result.NeedsTranslationPass);
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
