using System.IO;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Inference.Prompts;
using Microsoft.Extensions.Logging.Abstractions;

namespace IChing.Lab.Tests;

/// <summary>
/// 多模块组合 Prompt 单元测试：验证 ModuleFocuses 含 2 个及以上模块时，
/// TemplatePromptBuilder.Build 分别用各模块模板生成片段，再用 combined 模板拼装，
/// 最终 prompt 同时含格局与用神两个模块片段。
/// </summary>
public class MultiModulePromptTests
{
    [Fact]
    public void Build_MultipleModules_CombinesSnippetsIntoCombinedTemplate()
    {
        using var dir = new TempDir();
        // 格局模块片段模板
        File.WriteAllText(
            Path.Combine(dir.Path, "bazi-tier1-lunar-geju.txt"),
            "【格局片段】日主：{{ engine_algorithm_basis }}；结论：成格");
        // 用神模块片段模板
        File.WriteAllText(
            Path.Combine(dir.Path, "bazi-tier1-lunar-yongshen.txt"),
            "【用神片段】用神：木；喜：水");
        // 组合模板：用 module_snippets.{module} 占位拼装两模块片段
        File.WriteAllText(
            Path.Combine(dir.Path, "bazi-tier1-combined.txt"),
            """
            ## 一、格局分析
            {{ module_snippets.geju }}

            ## 二、用神分析
            {{ module_snippets.yongshen }}

            ## 三、综合结论
            请基于以上两模块给出综合结论。
            """);

        using var registry = new PromptTemplateRegistry(dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "bazi", 1, "bazi-tier1-default");

        var ctx = new PromptContext(
            Chart: new { x = 1 },
            RuleDigest: null,
            Question: null,
            Focus: "综合",
            MaxTokens: 256,
            Engine: new EngineMetadata(
                Source: "lunar-csharp",
                Version: "1.6.8",
                AlgorithmBasis: "6tail lunar-csharp 0001-9999 年",
                TemplateHint: "lunar",
                ModuleFocus: new[] { "geju", "yongshen" }),
            ModuleFocuses: new[] { "geju", "yongshen" });

        var result = builder.Build(ctx);

        // 最终 prompt 应同时含格局与用神两个模块片段。
        Assert.Contains("【格局片段】", result.PromptText);
        Assert.Contains("【用神片段】", result.PromptText);
        // 应含组合模板的结构标题。
        Assert.Contains("## 一、格局分析", result.PromptText);
        Assert.Contains("## 二、用神分析", result.PromptText);
        // 片段中的算法依据应被渲染。
        Assert.Contains("6tail lunar-csharp", result.PromptText);
    }

    /// <summary>组合模板缺失时，应回退到首个模块的单模块模板。</summary>
    [Fact]
    public void Build_CombinedMissing_FallsBackToFirstModule()
    {
        using var dir = new TempDir();
        File.WriteAllText(
            Path.Combine(dir.Path, "bazi-tier1-lunar-geju.txt"),
            "GEOJU-ONLY-{{ engine_algorithm_basis }}");
        File.WriteAllText(
            Path.Combine(dir.Path, "bazi-tier1-lunar-yongshen.txt"),
            "YONGSHEN-ONLY");
        // 不创建 bazi-tier1-combined.txt → 应回退到首个模块 geju。

        using var registry = new PromptTemplateRegistry(dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "bazi", 1, "bazi-tier1-default");

        var ctx = new PromptContext(
            Chart: new { x = 1 },
            RuleDigest: null,
            Question: null,
            Focus: "综合",
            MaxTokens: 256,
            Engine: new EngineMetadata(
                Source: "lunar-csharp",
                Version: "1.6.8",
                AlgorithmBasis: "6tail lunar-csharp",
                TemplateHint: "lunar",
                ModuleFocus: new[] { "geju", "yongshen" }),
            ModuleFocuses: new[] { "geju", "yongshen" });

        var result = builder.Build(ctx);

        // 回退到首个模块 geju 的片段，不应含 yongshen 片段。
        Assert.Contains("GEOJU-ONLY-", result.PromptText);
        Assert.DoesNotContain("YONGSHEN-ONLY", result.PromptText);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iching-multimodule-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch { /* 忽略清理失败 */ }
        }
    }
}
