using IChing.Lab.Api.Components;
using IChing.Lab.Inference;
using IChing.Lab.Core.Rules;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRazorComponents();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(new RuleEngine(ReadRuleEngineOptions(builder.Configuration)));

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

var modelPath = ResolveModelPath(
    builder.Configuration["Inference:ModelPath"] ?? "./models/qwen3-0.6b-genai",
    builder.Environment.ContentRootPath);
builder.Services.AddSingleton(sp =>
    new ChartInterpretationService(modelPath, sp.GetRequiredService<ILogger<ChartInterpretationService>>()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>();

app.Run();

static string ResolveModelPath(string configuredPath, string contentRoot)
{
    if (Path.IsPathRooted(configuredPath))
    {
        return configuredPath;
    }

    var candidates = new[]
    {
        Path.GetFullPath(configuredPath),
        Path.GetFullPath(Path.Combine(contentRoot, configuredPath)),
        Path.GetFullPath(Path.Combine(contentRoot, "..", "..", configuredPath))
    };

    return candidates.FirstOrDefault(Directory.Exists) ?? candidates[1];
}

static RuleEngineOptions ReadRuleEngineOptions(IConfiguration configuration)
{
    var section = configuration.GetSection("RuleEngine");
    var options = new RuleEngineOptions();
    if (int.TryParse(section["MinWeight"], out var minWeight))
    {
        options.MinWeight = minWeight;
    }

    foreach (var plugin in section.GetSection("Plugins").GetChildren())
    {
        options.Plugins[plugin.Key] = new RulePluginOptions
        {
            Enabled = bool.TryParse(plugin["Enabled"], out var enabled) ? enabled : null,
            Weight = int.TryParse(plugin["Weight"], out var weight) ? weight : null
        };
    }

    return options;
}

public partial class Program;
