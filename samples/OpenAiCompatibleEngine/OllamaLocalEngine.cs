using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OpenAiCompatibleEngine;

/// <summary>
/// Ollama 本地引擎（模式 B）。通过 Ollama 的 OpenAI 兼容端点 /v1/chat/completions 调用本地模型。
/// 默认 BaseUrl=http://localhost:11434，默认模型 qwen2.5:7b，均可由配置覆盖。
/// </summary>
public sealed class OllamaLocalEngine : OpenAiCompatibleEngineBase
{
    /// <inheritdoc />
    public override string EngineId => "ollama-local";

    private readonly HttpClient _client;
    private readonly string _model;
    private readonly ILogger<OllamaLocalEngine> _logger;

    /// <inheritdoc />
    protected override HttpClient Client => _client;

    /// <inheritdoc />
    protected override string ModelName => _model;

    /// <inheritdoc />
    protected override string ApiEndpoint => "/v1/chat/completions";

    /// <summary>
    /// 构造 Ollama 本地引擎。从 <paramref name="configuration"/> 读取
    /// Ollama:BaseUrl 与 Ollama:Model，缺失时使用默认值。
    /// </summary>
    public OllamaLocalEngine(IConfiguration configuration, ILogger<OllamaLocalEngine> logger)
    {
        _logger = logger;

        var baseUrl = configuration["Ollama:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://localhost:11434";
        }

        _model = configuration["Ollama:Model"];
        if (string.IsNullOrWhiteSpace(_model))
        {
            _model = "qwen2.5:7b";
        }

        _client = new HttpClient
        {
            BaseAddress = new Uri(EnsureTrailingSlash(baseUrl)),
            Timeout = TimeSpan.FromSeconds(100),
        };
    }

    /// <inheritdoc />
    /// <remarks>探活：GET /api/tags 返回 200 即认为 Ollama 服务已就绪。</remarks>
    protected override bool ProbeIsReady(CancellationToken ct)
    {
        try
        {
            using var response = _client.GetAsync("/api/tags", ct).GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama 探活失败（{BaseUrl}）。", _client.BaseAddress);
            return false;
        }
    }

    /// <summary>确保 URL 以 "/" 结尾，避免 HttpClient 相对 URI 解析异常。null/空视为根路径 "/"。</summary>
    private static string EnsureTrailingSlash(string? url) =>
        string.IsNullOrEmpty(url) ? "/" : (url.EndsWith('/') ? url : url + "/");
}
