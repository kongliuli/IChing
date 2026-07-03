using System.Diagnostics;
using IChing.Lab.Inference;
using Microsoft.Extensions.Logging;

var options = ParseArgs(args);
var fixtureDir = ResolveFixtureDir(options.FixtureDir);
var fixtures = ResolveFixtures(fixtureDir, options.FixtureId);

Console.WriteLine($"Fixture dir: {fixtureDir}");
Console.WriteLine($"Model path:  {options.ModelPath}");
Console.WriteLine($"Dry run:     {options.DryRun}");
Console.WriteLine();

if (fixtures.Count == 0)
{
    Console.Error.WriteLine("No fixtures found.");
    return 1;
}

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
var service = new ChartInterpretationService(options.ModelPath, loggerFactory.CreateLogger<ChartInterpretationService>());

var exitCode = 0;
foreach (var path in fixtures)
{
    var fixture = PromptFixtureLoader.Load(path);
    Console.WriteLine(new string('=', 72));
    Console.WriteLine($"Fixture: {fixture.Id} ({fixture.Domain}, tier {fixture.Tier})");
    Console.WriteLine($"File:    {path}");
    Console.WriteLine();

    var prompt = PromptFixtureLoader.BuildPrompt(fixture);
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
            var translatePrompt = IChing.Lab.Inference.Prompts.TarotPromptBuilder.BuildTranslateToChinese(
                pass1.Text,
                PromptFixtureLoader.GetTarotCardNames(fixture));
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
