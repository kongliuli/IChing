using IChing.Lab.Api.Components;
using IChing.Lab.Inference;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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
builder.Services.AddSingleton(sp =>
    new ChartInterpretationService(modelPath, sp.GetRequiredService<ILogger<ChartInterpretationService>>()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.UseAntiforgery();

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

public partial class Program;
