using System.Text.Json;
using IChing.Lab.Core.Rules;

namespace IChing.Lab.Api.Services;

public sealed class RuleEngineOptionsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public RuleEngineOptionsStore(IWebHostEnvironment environment)
    {
        var dataDir = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _path = Path.Combine(dataDir, "rule-engine-options.json");
    }

    public RuleEngineOptions Load(RuleEngineOptions fallback)
    {
        if (!File.Exists(_path))
        {
            return fallback;
        }

        try
        {
            var stored = JsonSerializer.Deserialize<RuleEngineOptions>(File.ReadAllText(_path));
            return stored ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    public void Save(RuleEngineOptions options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(options, JsonOptions));
    }
}
