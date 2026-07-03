using System.IO;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Engines.Bazi;
using IChing.Lab.Inference.Prompts;
using Microsoft.Extensions.Logging.Abstractions;

namespace IChing.Lab.Tests;

/// <summary>
/// cnlunar 引擎 prompt 集成测试：验证以 bazi-cnlunar-port 引擎的 EngineMetadata（TemplateHint="cnlunar"）
/// 驱动模板选择时，TemplatePromptBuilder 选用 bazi-tier1-cnlunar-default.txt，
/// 产出的 prompt 引用宜忌等第相关字段（建除 / 协纪辨方书）。
/// </summary>
public class CnlunarEnginePromptIntegrationTest
{
    [Fact]
    public void Build_WithCnlunarEngine_SelectsCnlunarTemplateReferencingYiJi()
    {
        // 构造 bazi-cnlunar-port 引擎实例，取其真实 EngineMetadata。
        var engine = new BaziCnlunarPortEngine();
        Assert.Equal("cnlunar", engine.Metadata.TemplateHint);

        // 用真实排盘结果作为 Chart，使集成测试贴近实际调用路径。
        var chart = engine.Calculate(new ChartRequest("bazi", new Dictionary<string, object?>
        {
            ["year"] = 2026,
            ["month"] = 3,
            ["day"] = 15,
            ["hour"] = 9,
            ["minute"] = 30,
            ["second"] = 0
        }));

        // 解析真实 prompts 目录（构建时由 Api csproj 复制到输出目录；测试环境回退到仓库根 prompts/）。
        var promptsDir = ResolvePromptsDir();
        using var registry = new PromptTemplateRegistry(promptsDir, NullLogger<PromptTemplateRegistry>.Instance);
        var builder = new TemplatePromptBuilder(registry, "bazi", 1, "bazi-tier1-default");

        // PromptContext 注入 cnlunar 引擎元数据；ModuleFocuses 留空，走单模板三级回退命中 cnlunar 默认模板。
        var ctx = new PromptContext(
            Chart: chart,
            RuleDigest: null,
            Question: null,
            Focus: "宜忌",
            MaxTokens: 256,
            Engine: engine.Metadata,
            ModuleFocuses: null);

        var result = builder.Build(ctx);

        // 模板 bazi-tier1-cnlunar-default.txt 含“建除”与“协纪辨方书”，命中即应渲染出来。
        Assert.Contains("建除", result.PromptText);
        Assert.Contains("协纪辨方书", result.PromptText);
        // 算法依据变量也应被注入。
        Assert.Contains(engine.Metadata.AlgorithmBasis, result.PromptText);
    }

    /// <summary>解析 prompts 模板目录：依次尝试输出目录、cwd、仓库根 prompts/。</summary>
    private static string ResolvePromptsDir()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "prompts"),
            Path.Combine(Directory.GetCurrentDirectory(), "prompts"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "prompts"),
            // 测试输出目录 bin/Debug/net10.0/ 向上 5 级到仓库根，再进 prompts/（开发环境回退）。
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "prompts")
        };

        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (Directory.Exists(full))
            {
                return full;
            }
        }

        return Path.GetFullPath(candidates[0]);
    }
}
