using Microsoft.Extensions.Configuration;

namespace IChing.Lab.Api.Commercial;

/// <summary>
/// 启动时把 CommercialAi 段解析为引擎可读的扁平配置（含环境变量 Key）。
/// </summary>
public static class CommercialAiBootstrap
{
    public static void Apply(ConfigurationManager configuration)
    {
        var section = configuration.GetSection(CommercialAiOptions.SectionName);
        var options = section.Get<CommercialAiOptions>() ?? new CommercialAiOptions();
        if (!options.Enabled)
        {
            return;
        }

        var apiKey = configuration["CommercialAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable(options.ApiKeyEnvironmentVariable);
        }

        var memory = new Dictionary<string, string?>
        {
            ["CommercialAi:Enabled"] = "true",
            ["CommercialAi:BaseUrl"] = options.BaseUrl,
            ["CommercialAi:Model"] = options.Model,
            ["CommercialAi:ApiKeyEnvironmentVariable"] = options.ApiKeyEnvironmentVariable,
            // 同时写入 DeepSeek/OpenAI 键，兼容现有插件引擎
            ["DeepSeek:BaseUrl"] = options.BaseUrl,
            ["DeepSeek:Model"] = options.Model,
            ["OpenAI:BaseUrl"] = options.BaseUrl,
            ["OpenAI:Model"] = options.Model,
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            memory["CommercialAi:ApiKey"] = apiKey;
            memory["DeepSeek:ApiKey"] = apiKey;
            memory["OpenAI:ApiKey"] = apiKey;
        }

        configuration.AddInMemoryCollection(memory);

        // 商业版优先远端链（Key 在服务端）；本地 ORT 仍可作降级。
        if (configuration.GetSection("plugins:fallbackChain").GetChildren().Any())
        {
            // 不覆盖已有 fallbackChain JSON 数组时，仅追加注释性默认：由 appsettings 声明。
        }
    }
}
