using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Api.Components;
using IChing.Lab.Api.Services;
using IChing.Lab.Core.Engines;
using IChing.Lab.Core.Services;
using IChing.Lab.Engines.Bazi;
using IChing.Lab.Engines.Calendar;
using IChing.Lab.Engines.Liuyao;
using IChing.Lab.Engines.Tarot;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Engines;
using IChing.Lab.Inference.Prompts;
using IChing.Lab.Core.Rules;
using IChing.Lab.PluginLoader;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RuleEngineOptionsStore>();
builder.Services.AddSingleton(sp =>
{
    var store = sp.GetRequiredService<RuleEngineOptionsStore>();
    return new RuleEngine(store.Load(ReadRuleEngineOptions(builder.Configuration)));
});
builder.Services.AddScoped<TarotDemoService>();
builder.Services.AddScoped<BaziDemoService>();
builder.Services.AddScoped<LiuyaoDemoService>();

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

var modelPath = ResolveModelPath(
    builder.Configuration["Inference:ModelPath"] ?? "./models/qwen3-0.6b-genai",
    builder.Environment.ContentRootPath);
builder.Services.AddSingleton<IInferenceEngine>(sp =>
    new OnnxGenAiEngine(modelPath, sp.GetRequiredService<ILogger<OnnxGenAiEngine>>()));
builder.Services.AddSingleton<IInferenceEngine, TemplateFallbackEngine>();
builder.Services.AddSingleton<ChartInterpretationOrchestrator>();

// 注册 Prompt 模板注册表（单例，FileSystemWatcher 热重载）+ 4 个 IPromptBuilder（按 domain+tier+templateId）。
// ChartInterpretationOrchestrator 与 LabController 通过 IEnumerable<IPromptBuilder> 注入并按 TemplateId 选取。
var promptsRoot = ResolvePromptsRoot(
    builder.Configuration["Prompts:TemplateRoot"] ?? "./prompts",
    builder.Environment.ContentRootPath);
builder.Services.AddSingleton(sp => new PromptTemplateRegistry(
    promptsRoot,
    sp.GetRequiredService<ILogger<PromptTemplateRegistry>>()));
builder.Services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
    sp.GetRequiredService<PromptTemplateRegistry>(), "bazi", 1, "bazi-tier1-default"));
builder.Services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
    sp.GetRequiredService<PromptTemplateRegistry>(), "liuyao", 1, "liuyao-tier1-default"));
builder.Services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
    sp.GetRequiredService<PromptTemplateRegistry>(), "tarot", 1, "tarot-tier1-en"));
builder.Services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
    sp.GetRequiredService<PromptTemplateRegistry>(), "tarot", 1, "tarot-translate-to-zh"));
builder.Services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
    sp.GetRequiredService<PromptTemplateRegistry>(), "tarot", 1, "tarot-tier1-deckaura-default"));
builder.Services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
    sp.GetRequiredService<PromptTemplateRegistry>(), "tarot", 2, "tarot-tier2-celtic-cross"));

// 注册排盘引擎包装类到 DI 容器（同 domain 可注册多个实现，按 EngineId 区分）
builder.Services.AddSingleton<IChartEngine, BaziChartEngine>();
builder.Services.AddSingleton<IChartEngine, LiuyaoChartEngine>();
builder.Services.AddSingleton<IChartEngine, TarotChartEngine>();
builder.Services.AddSingleton<IChartEngine, CalendarEngine>();

// 四域排盘插件（供 Lab API 与单机客户端共用同一套 IChartEngine 实现）
new BaziEnginesModule().Register(builder.Services);
new LiuyaoEnginesModule().Register(builder.Services);
new TarotEnginesModule().Register(builder.Services);
new CalendarEnginesModule().Register(builder.Services);
builder.Services.AddSingleton(sp => new ChartEngineRouter(sp.GetServices<IChartEngine>()));

// 加载外部插件（在 Build 之前将插件服务注册到 DI）。
// 共享接口 IPluginModule/IPluginManifest 落到 default ALC，保证主程序与插件类型同一；
// 任一插件加载失败仅记录日志并跳过，不影响主程序启动。
using var pluginLoggerFactory = LoggerFactory.Create(b =>
{
    b.AddConfiguration(builder.Configuration.GetSection("Logging"));
    b.AddConsole();
});
var pluginLoader = new PluginLoader(
    builder.Configuration,
    pluginLoggerFactory.CreateLogger<PluginLoader>(),
    builder.Environment.ContentRootPath);
if (!builder.Environment.IsEnvironment("Testing"))
{
    pluginLoader.DiscoverAndRegister(builder.Services);
}
builder.Services.AddSingleton(pluginLoader);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/", () => Results.Redirect("/demo"));
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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

// 解析 prompts 模板根目录：依次尝试 cwd、contentRoot、仓库根（contentRoot/../..）、输出目录。
// 找不到时返回首个候选路径，由 PromptTemplateRegistry 回退到内嵌默认模板并记录 warning。
static string ResolvePromptsRoot(string configuredPath, string contentRoot)
{
    if (Path.IsPathRooted(configuredPath))
    {
        return configuredPath;
    }

    var candidates = new[]
    {
        Path.GetFullPath(configuredPath),
        Path.GetFullPath(Path.Combine(contentRoot, configuredPath)),
        Path.GetFullPath(Path.Combine(contentRoot, "..", "..", configuredPath)),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath))
    };

    return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
}

static RuleEngineOptions ReadRuleEngineOptions(IConfiguration configuration)
{
    var options = new RuleEngineOptions();
    configuration.GetSection("RuleEngine").Bind(options);
    return options;
}

public partial class Program;
