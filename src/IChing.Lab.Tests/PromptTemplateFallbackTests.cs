using System.IO;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Inference.Prompts;
using Microsoft.Extensions.Logging.Abstractions;

namespace IChing.Lab.Tests;

/// <summary>
/// 算法感知模板三级回退单元测试：验证 TemplatePromptBuilder 在不同模板文件存在情况下，
/// 按 (domain, tier, engineVariant, module) 三级回退正确命中对应模板。
/// </summary>
public class PromptTemplateFallbackTests
{
    /// <summary>构造 bazi 领域 tier1 的 PromptContext，含 lunar 引擎与 yongshen 模块。</summary>
    private static PromptContext BuildBaziLunarYongshenCtx() => new(
        Chart: new { x = 1 },
        RuleDigest: null,
        Question: null,
        Focus: "用神",
        MaxTokens: 256,
        Engine: new EngineMetadata(
            Source: "lunar-csharp",
            Version: "1.6.8",
            AlgorithmBasis: "6tail lunar-csharp",
            TemplateHint: "lunar",
            ModuleFocus: new[] { "yongshen" }),
        ModuleFocuses: new[] { "yongshen" });

    /// <summary>Test1: 磁盘有 bazi-tier1-lunar-yongshen.txt → 命中第一级（engineVariant+module）。</summary>
    [Fact]
    public void Fallback_Level1_ModuleAndEngineVariant_HitsFirstLevel()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "bazi-tier1-lunar-yongshen.txt"), "L1-{{ focus }}");

        using var registry = new PromptTemplateRegistry(dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "bazi", 1, "bazi-tier1-default");

        var result = builder.Build(BuildBaziLunarYongshenCtx());

        Assert.Contains("L1-", result.PromptText);
        // 不应回退到内嵌默认（内嵌默认含“八字解读助手”字样）。
        Assert.DoesNotContain("八字解读助手", result.PromptText);
    }

    /// <summary>Test2: 磁盘只有 bazi-tier1-lunar.txt（bare 命名）→ 命中第二级（engineVariant only）。</summary>
    [Fact]
    public void Fallback_Level2_EngineVariantBare_HitsSecondLevel()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "bazi-tier1-lunar.txt"), "L2-{{ focus }}");

        using var registry = new PromptTemplateRegistry(dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "bazi", 1, "bazi-tier1-default");

        var result = builder.Build(BuildBaziLunarYongshenCtx());

        Assert.Contains("L2-", result.PromptText);
    }

    /// <summary>Test2b: 磁盘只有 bazi-tier1-lunar-default.txt（-default 命名，spec 场景）→ 命中第二级。</summary>
    [Fact]
    public void Fallback_Level2_EngineVariantDefault_HitsSecondLevel()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "bazi-tier1-lunar-default.txt"), "L2D-{{ focus }}");

        using var registry = new PromptTemplateRegistry(dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "bazi", 1, "bazi-tier1-default");

        var result = builder.Build(BuildBaziLunarYongshenCtx());

        Assert.Contains("L2D-", result.PromptText);
    }

    /// <summary>Test3: 磁盘只有 bazi-tier1-default.txt → 命中第三级（全局 default，向下兼容）。</summary>
    [Fact]
    public void Fallback_Level3_GlobalDefault_HitsThirdLevel()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "bazi-tier1-default.txt"), "L3-{{ focus }}");

        using var registry = new PromptTemplateRegistry(dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "bazi", 1, "bazi-tier1-default");

        var result = builder.Build(BuildBaziLunarYongshenCtx());

        Assert.Contains("L3-", result.PromptText);
    }

    /// <summary>Test4: 旧调用方（Engine=null）→ 命中 {TemplateId}，行为与改造前一致。</summary>
    [Fact]
    public void Fallback_NoEngine_FallsBackToTemplateId()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "bazi-tier1-default.txt"), "OLD-{{ focus }}");

        using var registry = new PromptTemplateRegistry(dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "bazi", 1, "bazi-tier1-default");

        // 旧调用方：不传 Engine 与 ModuleFocuses（默认 null）。
        var ctx = new PromptContext(
            Chart: new { x = 1 },
            RuleDigest: null,
            Question: null,
            Focus: "综合",
            MaxTokens: 256);

        var result = builder.Build(ctx);

        Assert.Contains("OLD-", result.PromptText);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iching-fallback-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch { /* 忽略清理失败 */ }
        }
    }
}
