using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OpenAiCompatibleEngine;

/// <summary>
/// Azure OpenAI 远程引擎（模式 C）。通过 Azure 资源专属端点调用：
/// <c>{baseUrl}/openai/deployments/{deployment}/chat/completions?api-version=2024-02-01</c>。
/// 鉴权使用 <c>api-key</c> 请求头，密钥由配置 Azure:ApiKey 提供（建议 User Secrets），绝不硬编码或入仓。
/// </summary>
public sealed class AzureOpenAiEngine : OpenAiCompatibleEngineBase
{
    /// <inheritdoc />
    public override string EngineId => "azure-openai-remote";

    private readonly HttpClient _client;
    private readonly string _deployment;
    private readonly ILogger<AzureOpenAiEngine> _logger;

    /// <inheritdoc />
    protected override HttpClient Client => _client;

    /// <inheritdoc />
    /// <remarks>Azure 以 deployment 名作为模型标识。</remarks>
    protected override string ModelName => _deployment;

    /// <inheritdoc />
    protected override string ApiEndpoint => $"/openai/deployments/{_deployment}/chat/completions?api-version=2024-02-01";

    /// <summary>
    /// 构造 Azure OpenAI 远程引擎。从 <paramref name="configuration"/> 读取
    /// Azure:BaseUrl / Azure:Deployment / Azure:ApiKey。
    /// </summary>
    public AzureOpenAiEngine(IConfiguration configuration, ILogger<AzureOpenAiEngine> logger)
    {
        _logger = logger;

        var baseUrl = configuration["Azure:BaseUrl"];
        _deployment = configuration["Azure:Deployment"] ?? string.Empty;

        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

        // 仅在 BaseUrl 提供时设置 BaseAddress；缺失时请求会失败并触发降级。
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _client.BaseAddress = new Uri(EnsureTrailingSlash(baseUrl));
        }

        // API key 仅从配置读取并写入请求头，绝不硬编码或记录日志。
        var apiKey = configuration["Azure:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _client.DefaultRequestHeaders.Add("api-key", apiKey);
        }
        else
        {
            logger.LogWarning("未配置 Azure:ApiKey，Azure OpenAI 引擎调用将返回 401 并触发降级。");
        }
    }

    /// <inheritdoc />
    /// <remarks>远程 API 不主动探活，调用失败时由 GenerateAsync 触发降级。</remarks>
    protected override bool ProbeIsReady(CancellationToken ct) => true;

    /// <summary>确保 URL 以 "/" 结尾，避免 HttpClient 相对 URI 解析异常。null/空视为根路径 "/"。</summary>
    private static string EnsureTrailingSlash(string? url) =>
        string.IsNullOrEmpty(url) ? "/" : (url.EndsWith('/') ? url : url + "/");
}
