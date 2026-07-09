using Microsoft.Extensions.Configuration;

namespace IChing.Lab.Api.Services;

public static class LabChartEngineConfig
{
    public static string? ResolveDefaultEngineId(IConfiguration configuration)
    {
        foreach (var child in configuration.GetSection("plugins:inferenceEngines").GetChildren())
        {
            if (string.Equals(child["default"], "true", StringComparison.OrdinalIgnoreCase))
            {
                return child["id"];
            }
        }

        return configuration["plugins:defaultEngine"];
    }

    public static string? ResolveChartEngine(IConfiguration configuration, string domain)
    {
        foreach (var child in configuration.GetSection("plugins:chartEngines").GetChildren())
        {
            if (string.Equals(child["domain"], domain, StringComparison.OrdinalIgnoreCase))
            {
                return child["default"];
            }
        }

        return null;
    }
}
