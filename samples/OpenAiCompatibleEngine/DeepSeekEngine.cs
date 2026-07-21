using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OpenAiCompatibleEngine;

/// <summary>
/// DeepSeek 远程引擎（模式 C）。通过 https://api.deepseek.com/v1/chat/completions 调用 DeepSeek 官方接口。
/// </summary>
/// <remarks>
/// API Key 读取顺序：<c>CommercialAi:ApiKey</c>（商业版服务端）→ <c>DeepSeek:ApiKey</c>（User Secrets / 环境变量）。
/// 绝不入仓。
/// </remarks>
public sealed class DeepSeekEngine : OpenAiCompatibleEngineBase
{
    public override string EngineId => "deepseek-remote";

    private readonly HttpClient _client;
    private readonly string _model;
    private readonly ILogger<DeepSeekEngine> _logger;

    protected override HttpClient Client => _client;
    protected override string ModelName => _model;
    protected override string ApiEndpoint => "/v1/chat/completions";

    public DeepSeekEngine(IConfiguration configuration, ILogger<DeepSeekEngine> logger)
    {
        _logger = logger;

        var baseUrl = configuration["CommercialAi:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl) ||
            !string.Equals(configuration["CommercialAi:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = configuration["DeepSeek:BaseUrl"];
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://api.deepseek.com/v1";
        }

        var model = configuration["CommercialAi:Model"];
        if (string.IsNullOrWhiteSpace(model) ||
            !string.Equals(configuration["CommercialAi:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
        {
            model = configuration["DeepSeek:Model"];
        }

        _model = string.IsNullOrWhiteSpace(model) ? "deepseek-chat" : model;

        _client = new HttpClient
        {
            BaseAddress = new Uri(EnsureTrailingSlash(baseUrl)),
            Timeout = TimeSpan.FromSeconds(60),
        };

        var apiKey = ResolveApiKey(configuration);
        if (!string.IsNullOrEmpty(apiKey))
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        else
        {
            logger.LogWarning("未配置 CommercialAi:ApiKey / DeepSeek:ApiKey，DeepSeek 远程引擎将触发降级。");
        }
    }

    /// <summary>测试专用：注入 mock handler。</summary>
    internal DeepSeekEngine(HttpMessageHandler handler, ILogger<DeepSeekEngine> logger, string? apiKey = null)
    {
        _logger = logger;
        _model = "deepseek-chat";
        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.deepseek.com/v1/"),
            Timeout = TimeSpan.FromSeconds(60),
        };

        var key = apiKey ?? "sk-test-only";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    }

    protected override bool ProbeIsReady(CancellationToken ct) => true;

    protected override void Dispose(bool disposing) => base.Dispose(disposing);

    internal static string? ResolveApiKey(IConfiguration configuration)
    {
        if (string.Equals(configuration["CommercialAi:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
        {
            var commercial = configuration["CommercialAi:ApiKey"];
            if (!string.IsNullOrWhiteSpace(commercial))
            {
                return commercial;
            }

            var envName = configuration["CommercialAi:ApiKeyEnvironmentVariable"] ?? "CommercialAi__ApiKey";
            var fromEnv = Environment.GetEnvironmentVariable(envName);
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv;
            }
        }

        return configuration["DeepSeek:ApiKey"];
    }

    private static string EnsureTrailingSlash(string? url) =>
        string.IsNullOrEmpty(url) ? "/" : (url.EndsWith('/') ? url : url + "/");
}
