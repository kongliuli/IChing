using System.Net;
using System.Text;
using System.Text.Json;
using IChing.Lab.Abstractions.Models;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAiCompatibleEngine;
// 同时存在 IChing.Lab.Inference.GenerationResult 与 IChing.Lab.Abstractions.Models.GenerationResult，
// 此处以别名显式绑定到抽象层模型，避免 CS0104 歧义。
using GenerationResult = IChing.Lab.Abstractions.Models.GenerationResult;
using GenerateOptions = IChing.Lab.Abstractions.Models.GenerateOptions;

namespace IChing.Lab.Tests;

/// <summary>
/// DeepSeek 远程引擎单元测试。
/// 通过 mock HttpMessageHandler 验证请求体（model）、Authorization 头以及
/// 响应解析路径（choices[0].message.content）与 HTTP 5xx 降级行为。
/// </summary>
public class DeepSeekEngineTests
{
    private const string ExpectedApiKey = "unit-test-api-key";
    private const string ExpectedModel = "deepseek-chat";

    /// <summary>
    /// 成功响应应解析 choices[0].message.content 返回 IsFallback=false，
    /// 且请求体含 model="deepseek-chat"、Authorization 头为 "Bearer {apiKey}"。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_Success_ParsesContentAndSendsCorrectRequest()
    {
        var capturer = new RequestCapturer(HttpStatusCode.OK, """
            {
              "id": "chatcmpl-test",
              "object": "chat.completion",
              "choices": [
                { "index": 0, "message": { "role": "assistant", "content": "乾为天，刚健中正。" }, "finish_reason": "stop" }
              ],
              "usage": { "prompt_tokens": 10, "completion_tokens": 8, "total_tokens": 18 }
            }
            """);

        using var engine = new DeepSeekEngine(capturer, NullLogger<DeepSeekEngine>.Instance, ExpectedApiKey);

        var result = await engine.GenerateAsync("解释乾卦", new GenerateOptions(MaxTokens: 64), CancellationToken.None);

        // 响应解析
        Assert.Equal("deepseek-remote", result.EngineId);
        Assert.False(result.IsFallback);
        Assert.Equal("乾为天，刚健中正。", result.Text);
        Assert.Null(result.FallbackReason);

        // 请求验证：恰好发送一次 POST /v1/chat/completions
        Assert.Single(capturer.Captured);
        var captured = capturer.Captured[0];
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal("https://api.deepseek.com/v1/chat/completions", captured.RequestUri?.ToString());

        // Authorization 头
        Assert.Equal($"Bearer {ExpectedApiKey}", captured.Authorization);

        // 请求体含 model="deepseek-chat"
        Assert.NotNull(captured.Body);
        using var doc = JsonDocument.Parse(captured.Body);
        Assert.True(doc.RootElement.TryGetProperty("model", out var modelEl));
        Assert.Equal(ExpectedModel, modelEl.GetString());
    }

    /// <summary>HTTP 500 响应应返回 IsFallback=true 且 FallbackReason 描述错误码。</summary>
    [Fact]
    public async Task GenerateAsync_Http500_ReturnsFallbackTrue()
    {
        var capturer = new RequestCapturer(HttpStatusCode.InternalServerError, "upstream error");

        using var engine = new DeepSeekEngine(capturer, NullLogger<DeepSeekEngine>.Instance, ExpectedApiKey);

        var result = await engine.GenerateAsync("解释坤卦", new GenerateOptions(MaxTokens: 64), CancellationToken.None);

        Assert.Equal("deepseek-remote", result.EngineId);
        Assert.True(result.IsFallback);
        Assert.Empty(result.Text);
        Assert.Contains("500", result.FallbackReason ?? string.Empty);
    }

    /// <summary>
    /// 简易 HttpMessageHandler：在请求到达时立即读取并缓存请求体/鉴权头/方法/URI，
    /// 避免请求在引擎内部 Dispose 后无法访问。仅用于 DeepSeekEngine 单元测试。
    /// </summary>
    private sealed class RequestCapturer : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _responseBody;

        public RequestCapturer(HttpStatusCode status, string responseBody)
        {
            _status = status;
            _responseBody = responseBody;
            Captured = new List<CapturedRequest>();
        }

        public List<CapturedRequest> Captured { get; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 引擎内部会在请求完成后 Dispose 请求内容，必须在 SendAsync 内即时读取。
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Captured.Add(new CapturedRequest(
                request.Method,
                request.RequestUri?.ToString() ?? string.Empty,
                request.Headers.Authorization?.ToString() ?? string.Empty,
                body));

            var content = new StringContent(_responseBody, Encoding.UTF8, "application/json");
            return new HttpResponseMessage(_status) { Content = content };
        }
    }

    /// <summary>已捕获的请求快照，避免依赖被引擎 Dispose 的 HttpRequestMessage。</summary>
    private sealed record CapturedRequest(HttpMethod Method, string RequestUri, string Authorization, string? Body);
}
