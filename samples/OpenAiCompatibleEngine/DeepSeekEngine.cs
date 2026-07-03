using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace OpenAiCompatibleEngine;

/// <summary>
/// DeepSeek 远程引擎（模式 C）。通过 https://api.deepseek.com/v1/chat/completions 调用 DeepSeek 官方接口。
/// 默认模型 deepseek-chat。
/// </summary>
/// <remarks>
/// 当前实现硬编码测试 API key（TEST-ONLY），仅供沙箱/CI 联调使用。
/// 生产环境须改为通过 <c>IConfiguration</c> 读取 <c>DeepSeek:ApiKey</c>（建议使用 User Secrets：
/// <c>dotnet user-secrets set "DeepSeek:ApiKey" "..."</c>），绝不入仓。
/// </remarks>
public sealed class DeepSeekEngine : OpenAiCompatibleEngineBase
{
    /// <inheritdoc />
    public override string EngineId => "deepseek-remote";

    private readonly HttpClient _client;
    private readonly ILogger<DeepSeekEngine> _logger;

    /// <inheritdoc />
    protected override HttpClient Client => _client;

    /// <inheritdoc />
    protected override string ModelName => "deepseek-chat";

    /// <inheritdoc />
    /// <remarks>
    /// 以 "/" 开头相对 BaseAddress 的 host 根解析：即使 BaseUrl 含 /v1，
    /// 实际请求路径仍为 /v1/chat/completions，不会出现 /v1/v1 重复。
    /// </remarks>
    protected override string ApiEndpoint => "/v1/chat/completions";

    /// <summary>
    /// 构造 DeepSeek 远程引擎。BaseAddress 固定为 https://api.deepseek.com/v1/，
    /// 默认 Authorization 头使用下方 TEST-ONLY 硬编码 key。
    /// </summary>
    public DeepSeekEngine(ILogger<DeepSeekEngine> logger)
    {
        _logger = logger;

        _client = new HttpClient
        {
            BaseAddress = new Uri("https://api.deepseek.com/v1/"),
            Timeout = TimeSpan.FromSeconds(60),
        };

        // TEST-ONLY: 硬编码测试 key，生产环境须改为 IConfiguration + User Secrets（apiKeyKey="DeepSeek:ApiKey"）
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "sk-2c248bc685c144739c88181fb665d89d");
    }

    /// <summary>
    /// 测试专用构造函数：注入自定义 <see cref="HttpMessageHandler"/> 以模拟远程响应。
    /// 仅对单元测试程序集可见，不参与 DI 解析。
    /// </summary>
    internal DeepSeekEngine(HttpMessageHandler handler, ILogger<DeepSeekEngine> logger)
    {
        _logger = logger;

        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.deepseek.com/v1/"),
            Timeout = TimeSpan.FromSeconds(60),
        };

        // TEST-ONLY: 硬编码测试 key，生产环境须改为 IConfiguration + User Secrets（apiKeyKey="DeepSeek:ApiKey"）
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "sk-2c248bc685c144739c88181fb665d89d");
    }

    /// <inheritdoc />
    /// <remarks>远程 API 不主动探活（避免无谓的计费/限流请求），调用失败时由 GenerateAsync 触发降级。</remarks>
    protected override bool ProbeIsReady(CancellationToken ct) => true;

    /// <inheritdoc />
    /// <remarks>调用基类 Dispose(true) 释放 <see cref="Client"/>（HttpClient）。</remarks>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
