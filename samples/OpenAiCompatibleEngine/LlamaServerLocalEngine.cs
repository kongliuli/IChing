using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OpenAiCompatibleEngine;

/// <summary>
/// llama-server 本地引擎（模式 B）。llama.cpp 自带的 server 暴露 OpenAI 兼容端点 /v1/chat/completions。
/// 默认 BaseUrl=http://localhost:8080，模型由配置 LlamaServer:Model 指定。
/// </summary>
public sealed class LlamaServerLocalEngine : OpenAiCompatibleEngineBase
{
    /// <inheritdoc />
    public override string EngineId => "llama-server-local";

    private readonly HttpClient _client;
    private readonly string _model;
    private readonly ILogger<LlamaServerLocalEngine> _logger;

    /// <inheritdoc />
    protected override HttpClient Client => _client;

    /// <inheritdoc />
    protected override string ModelName => _model;

    /// <inheritdoc />
    protected override string ApiEndpoint => "/v1/chat/completions";

    /// <summary>
    /// 构造 llama-server 本地引擎。从 <paramref name="configuration"/> 读取
    /// LlamaServer:BaseUrl 与 LlamaServer:Model，缺失时使用默认值。
    /// </summary>
    public LlamaServerLocalEngine(IConfiguration configuration, ILogger<LlamaServerLocalEngine> logger)
    {
        _logger = logger;

        var baseUrl = configuration["LlamaServer:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://localhost:8080";
        }

        var model = configuration["LlamaServer:Model"];
        _model = string.IsNullOrWhiteSpace(model) ? "local-model" : model;

        _client = new HttpClient
        {
            BaseAddress = new Uri(EnsureTrailingSlash(baseUrl)),
            Timeout = TimeSpan.FromSeconds(100),
        };
    }

    /// <inheritdoc />
    /// <remarks>探活：GET /v1/models 返回 200 即认为 llama-server 已就绪。</remarks>
    protected override bool ProbeIsReady(CancellationToken ct)
    {
        try
        {
            using var response = _client.GetAsync("/v1/models", ct).GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "llama-server 探活失败（{BaseUrl}）。", _client.BaseAddress);
            return false;
        }
    }

    /// <summary>确保 URL 以 "/" 结尾，避免 HttpClient 相对 URI 解析异常。null/空视为根路径 "/"。</summary>
    private static string EnsureTrailingSlash(string? url) =>
        string.IsNullOrEmpty(url) ? "/" : (url.EndsWith('/') ? url : url + "/");
}
