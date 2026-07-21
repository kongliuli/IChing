using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OpenAiCompatibleEngine;

/// <summary>
/// OpenAI 远程引擎（模式 C）。通过 https://api.openai.com/v1/chat/completions 调用官方 OpenAI 接口。
/// 默认 BaseUrl=https://api.openai.com/v1，默认模型 gpt-4o-mini。
/// API key 必须由配置 OpenAI:ApiKey 提供（建议使用 User Secrets：<c>dotnet user-secrets set "OpenAI:ApiKey" "..."</c>），
/// 绝不硬编码于源码或入仓。
/// </summary>
public sealed class OpenAiRemoteEngine : OpenAiCompatibleEngineBase
{
    /// <inheritdoc />
    public override string EngineId => "openai-remote";

    private readonly HttpClient _client;
    private readonly string _model;
    private readonly ILogger<OpenAiRemoteEngine> _logger;

    /// <inheritdoc />
    protected override HttpClient Client => _client;

    /// <inheritdoc />
    protected override string ModelName => _model;

    /// <inheritdoc />
    /// <remarks>
    /// 以 "/" 开头相对 BaseAddress 的 host 根解析：即使 BaseUrl 含 /v1，
    /// 实际请求路径仍为 /v1/chat/completions，不会出现 /v1/v1 重复。
    /// </remarks>
    protected override string ApiEndpoint => "/v1/chat/completions";

    /// <summary>
    /// 构造 OpenAI 远程引擎。从 <paramref name="configuration"/> 读取
    /// OpenAI:BaseUrl / OpenAI:Model / OpenAI:ApiKey。
    /// </summary>
    public OpenAiRemoteEngine(IConfiguration configuration, ILogger<OpenAiRemoteEngine> logger)
    {
        _logger = logger;

        var baseUrl = configuration["OpenAI:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://api.openai.com/v1";
        }

        var model = configuration["OpenAI:Model"];
        _model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;

        _client = new HttpClient
        {
            BaseAddress = new Uri(EnsureTrailingSlash(baseUrl)),
            Timeout = TimeSpan.FromSeconds(60),
        };

        // API key 仅从配置读取并写入请求头，绝不硬编码或记录日志。
        var apiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        else
        {
            logger.LogWarning("未配置 OpenAI:ApiKey，OpenAI 远程引擎调用将返回 401 并触发降级。");
        }
    }

    /// <inheritdoc />
    /// <remarks>远程 API 不主动探活（避免无谓的计费/限流请求），调用失败时由 GenerateAsync 触发降级。</remarks>
    protected override bool ProbeIsReady(CancellationToken ct) => true;

    /// <summary>确保 URL 以 "/" 结尾，避免 HttpClient 相对 URI 解析异常。null/空视为根路径 "/"。</summary>
    private static string EnsureTrailingSlash(string? url) =>
        string.IsNullOrEmpty(url) ? "/" : (url.EndsWith('/') ? url : url + "/");
}
