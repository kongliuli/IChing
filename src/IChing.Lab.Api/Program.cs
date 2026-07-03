using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Api.Components;
using IChing.Lab.Core.Engines;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Engines;
using IChing.Lab.Inference.Prompts;
using IChing.Lab.PluginLoader;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRazorComponents();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// 注册排盘引擎包装类到 DI 容器（同 domain 可注册多个实现，按 EngineId 区分）
builder.Services.AddSingleton<IChartEngine, BaziChartEngine>();
builder.Services.AddSingleton<IChartEngine, LiuyaoChartEngine>();
builder.Services.AddSingleton<IChartEngine, TarotChartEngine>();
builder.Services.AddSingleton<IChartEngine, CalendarEngine>();

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
pluginLoader.DiscoverAndRegister(builder.Services);
builder.Services.AddSingleton(pluginLoader);

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

public partial class Program;
