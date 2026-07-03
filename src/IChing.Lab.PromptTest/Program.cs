using System.Diagnostics;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Engines;
using IChing.Lab.Inference.Prompts;
using Microsoft.Extensions.Logging;

var options = ParseArgs(args);
var fixtureDir = ResolveFixtureDir(options.FixtureDir);
var promptsDir = ResolvePromptsDir();
var fixtures = ResolveFixtures(fixtureDir, options.FixtureId);

Console.WriteLine($"Fixture dir: {fixtureDir}");
Console.WriteLine($"Prompts dir: {promptsDir}");
Console.WriteLine($"Model path:  {options.ModelPath}");
Console.WriteLine($"Dry run:     {options.DryRun}");
Console.WriteLine();

if (fixtures.Count == 0)
{
    Console.Error.WriteLine("No fixtures found.");
    return 1;
}

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
IInferenceEngine[] engines =
[
    new OnnxGenAiEngine(options.ModelPath, loggerFactory.CreateLogger<OnnxGenAiEngine>()),
    new TemplateFallbackEngine()
];
// 构造模板注册表 + 4 个 IPromptBuilder，供 orchestrator 按 templateId 选取。
using var registry = new PromptTemplateRegistry(promptsDir, loggerFactory.CreateLogger<PromptTemplateRegistry>());
IPromptBuilder[] promptBuilders =
[
    new TemplatePromptBuilder(registry, "bazi", 1, "bazi-tier1-default"),
    new TemplatePromptBuilder(registry, "liuyao", 1, "liuyao-tier1-default"),
    new TemplatePromptBuilder(registry, "tarot", 1, "tarot-tier1-en"),
    new TemplatePromptBuilder(registry, "tarot", 1, "tarot-translate-to-zh")
];
var promptBuilderIndex = promptBuilders.ToDictionary(b => b.TemplateId);
var service = new ChartInterpretationOrchestrator(engines, promptBuilders, loggerFactory.CreateLogger<ChartInterpretationOrchestrator>());

var exitCode = 0;
foreach (var path in fixtures)
{
    var fixture = PromptFixtureLoader.Load(path);
    Console.WriteLine(new string('=', 72));
    Console.WriteLine($"Fixture: {fixture.Id} ({fixture.Domain}, tier {fixture.Tier})");
    Console.WriteLine($"File:    {path}");
    Console.WriteLine();

    // dry-run 与生成路径统一通过 IPromptBuilder（按 fixture 推导的 templateId）构建 prompt。
    var templateId = PromptFixtureLoader.ResolveTemplateId(fixture);
    var builder = promptBuilderIndex[templateId];
    var promptResult = PromptFixtureLoader.BuildPrompt(fixture, builder);
    var prompt = promptResult.PromptText;
    Console.WriteLine("--- PROMPT ---");
    Console.WriteLine(prompt);
    Console.WriteLine("--- END PROMPT ---");
    Console.WriteLine();

    if (options.DryRun)
    {
        continue;
    }

    var totalSw = Stopwatch.StartNew();
    if (PromptFixtureLoader.NeedsTranslation(fixture))
    {
        var englishPrompt = prompt;
        var pass1 = service.Generate(englishPrompt, PromptFixtureLoader.GetMaxTokens(fixture));
        Console.WriteLine($"Pass1 (EN) [{pass1.ElapsedMs} ms] fallback={pass1.IsFallback}");
        Console.WriteLine(pass1.Text);
        Console.WriteLine();

        if (!pass1.IsFallback && !string.IsNullOrWhiteSpace(pass1.Text))
        {
            // 翻译 pass 改用 tarot-translate-to-zh 的 IPromptBuilder 构建翻译 prompt。
            var translateCtx = new PromptContext(
                Chart: pass1.Text,
                RuleDigest: PromptFixtureLoader.GetTarotCardNames(fixture),
                Question: null,
                Focus: null,
                MaxTokens: PromptFixtureLoader.GetTranslateMaxTokens(fixture));
            var translatePrompt = promptBuilderIndex["tarot-translate-to-zh"].Build(translateCtx).PromptText;
            var pass2 = service.Generate(translatePrompt, PromptFixtureLoader.GetTranslateMaxTokens(fixture));
            Console.WriteLine($"Pass2 (ZH) [{pass2.ElapsedMs} ms] fallback={pass2.IsFallback}");
            Console.WriteLine(pass2.Text);
        }
    }
    else
    {
        var result = service.RunFixture(fixture);
        Console.WriteLine($"Result fallback={result.IsFallback}");
        Console.WriteLine(result.Text);
        if (result.TextEn is not null)
        {
            Console.WriteLine();
            Console.WriteLine("--- EN ---");
            Console.WriteLine(result.TextEn);
        }
    }

    totalSw.Stop();
    Console.WriteLine();
    Console.WriteLine($"Total: {totalSw.ElapsedMilliseconds} ms");
    Console.WriteLine();

    if (!options.DryRun && !service.IsModelLoaded)
    {
        Console.Error.WriteLine("WARN: model not loaded — run scripts/download-qwen-15b-model.sh");
        exitCode = 2;
    }
}

return exitCode;

static string ResolveFixtureDir(string? dir)
{
    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
    {
        return Path.GetFullPath(dir);
    }

    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), "docs", "prompts", "fixtures"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "docs", "prompts", "fixtures"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "prompts", "fixtures")
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

// 解析 prompts/ 模板目录：优先仓库根 prompts/，其次输出目录下复制的 prompts/。
// 找不到时返回仓库根相对路径，由 PromptTemplateRegistry 回退到内嵌默认模板。
static string ResolvePromptsDir()
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), "prompts"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "prompts"),
        Path.Combine(AppContext.BaseDirectory, "prompts"),
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

static List<string> ResolveFixtures(string dir, string? id)
{
    if (!Directory.Exists(dir))
    {
        return [];
    }

    var all = Directory.GetFiles(dir, "*.json").OrderBy(f => f).ToList();
    if (string.IsNullOrWhiteSpace(id))
    {
        return all;
    }

    return all.Where(f => Path.GetFileNameWithoutExtension(f).Equals(id, StringComparison.OrdinalIgnoreCase)).ToList();
}

static Options ParseArgs(string[] args)
{
    var modelPath = "./models/qwen2.5-1.5b-genai";
    string? fixtureDir = null;
    string? fixtureId = null;
    var dryRun = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--model" when i + 1 < args.Length:
                modelPath = args[++i];
                break;
            case "--fixtures" when i + 1 < args.Length:
                fixtureDir = args[++i];
                break;
            case "--fixture" when i + 1 < args.Length:
                fixtureId = args[++i];
                break;
            case "--dry-run":
                dryRun = true;
                break;
        }
    }

    return new Options(modelPath, fixtureDir, fixtureId, dryRun);
}

record Options(string ModelPath, string? FixtureDir, string? FixtureId, bool DryRun);
