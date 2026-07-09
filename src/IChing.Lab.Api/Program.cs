using IChing.Lab.Api.Components;
using IChing.Lab.Api.Services;
using IChing.Lab.Composition;
using IChing.Lab.Core.Rules;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(nameof(AccountsCreditsGateway));
builder.Services.AddSingleton<AccountsCreditsGateway>();
builder.Services.AddSingleton<LabChartQueryService>();
builder.Services.AddSingleton<LabReadService>();
builder.Services.AddSingleton<LabHealthService>();

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

var modelPath = LabPathResolver.ResolveModelPath(
    builder.Configuration["Inference:ModelPath"] ?? "./models/qwen3-0.6b-genai",
    builder.Environment.ContentRootPath);
var promptsRoot = LabPathResolver.ResolvePromptsRoot(
    builder.Configuration["Prompts:TemplateRoot"] ?? "./prompts",
    builder.Environment.ContentRootPath);

builder.Services.AddLabChartEngines();
builder.Services.AddLabInference(modelPath, promptsRoot);

using var pluginLoggerFactory = LoggerFactory.Create(b =>
{
    b.AddConfiguration(builder.Configuration.GetSection("Logging"));
    b.AddConsole();
});
builder.Services.AddLabExternalPlugins(
    builder.Configuration,
    builder.Environment.ContentRootPath,
    pluginLoggerFactory,
    skipDiscovery: builder.Environment.IsEnvironment("Testing"));

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

static RuleEngineOptions ReadRuleEngineOptions(IConfiguration configuration)
{
    var options = new RuleEngineOptions();
    configuration.GetSection("RuleEngine").Bind(options);
    return options;
}

public partial class Program;
