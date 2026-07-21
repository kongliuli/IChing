using IChing.Lab.Client;
using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;
using IChing.Client.Shared.Security;
using IChing.Client.Shared.Settings;

namespace IChing.Client.Shared.Providers;

/// <summary>
/// 纯规则 / 加厚 Tier0：无 LLM。
/// </summary>
public sealed class RuleOnlyProvider : IInterpretationProvider
{
    public string ProviderId => "rule-only";
    public bool SupportsFollowUp => false;

    public Task<InterpretationResult> InterpretAsync(
        InterpretationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var question = PromptInputSanitizer.SanitizeUserText(request.Question);
        var focus = PromptInputSanitizer.SanitizeUserText(request.Focus);

        var text = RichRuleReading.Build(request.Domain, request.Chart, request.RuleDigest, question, focus);
        return Task.FromResult(new InterpretationResult(text, IsFallback: true, Error: null));
    }

    public Task<ConnectionTestResult> TestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new ConnectionTestResult(true, "规则引擎就绪（无需网络）"));

    public async IAsyncEnumerable<string> StreamFollowUpAsync(
        IReadOnlyList<ChatTurn> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }
}

/// <summary>
/// 用户自带 Key 的 OpenAI 兼容远端。
/// </summary>
public sealed class ByokRemoteProvider : IInterpretationProvider
{
    private readonly IClientRuntimeSettings _settings;

    public ByokRemoteProvider(IClientRuntimeSettings settings) => _settings = settings;

    public string ProviderId => "byok-remote";
    public bool SupportsFollowUp => true;

    public async Task<InterpretationResult> InterpretAsync(
        InterpretationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            return new InterpretationResult(
                string.Empty,
                true,
                "请先在设置中填写 API Key，或选择本机 Ollama 等本地端点");
        }

        var domainLabel = request.Domain switch
        {
            "bazi" => "八字",
            "liuyao" => "六爻",
            "tarot" => "塔罗",
            _ => request.Domain
        };

        if (request.FallbackPacket is not null)
        {
            var packet = SanitizePacket(request.FallbackPacket);
            return await SendAsync(
                domainLabel,
                ReadingPromptProtocol.BuildUserMessage(packet),
                ReadingPromptProtocol.BuildSystemPrompt(packet),
                packet.Mode,
                structured: true,
                cancellationToken);
        }

        var prompt = ReadingSummaries.BuildChatPrompt(
            domainLabel,
            PromptInputSanitizer.SanitizeUserText(request.Question),
            PromptInputSanitizer.SanitizeUserText(request.Focus),
            request.Chart ?? new { },
            request.RuleDigest);
        return await SendAsync(domainLabel, prompt, null, null, structured: false, cancellationToken);
    }

    public async Task<ConnectionTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendAsync("连通性", "请只回复 pong。", null, null, structured: false, cancellationToken);
        return result.IsFallback
            ? new ConnectionTestResult(false, result.Error)
            : new ConnectionTestResult(true, "远端 API 可达");
    }

    public async IAsyncEnumerable<string> StreamFollowUpAsync(
        IReadOnlyList<ChatTurn> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Qwen3.5 流式会把 token 耗在 reasoning、content 长期为空；本机改非流式一次返回
        if (OpenAiEndpointHelpers.IsLocalBaseUrl(_settings.BaseUrl))
        {
            var maxTokens = Math.Clamp(_settings.MaxTokens, 512, 2048);
            using var response = await OpenAiCompatibleChatClient.SendAsync(
                _settings,
                new OpenAiChatRequest(messages, maxTokens, _settings.Temperature, Stream: false),
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                yield return $"{(int)response.StatusCode}: {OpenAiCompatibleChatClient.Trim(body, 180)}";
                yield break;
            }

            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var text = OpenAiCompatibleChatClient.ExtractMessageContent(doc.RootElement) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }

            yield break;
        }

        var remoteMax = Math.Min(_settings.MaxTokens, 350);
        await foreach (var chunk in OpenAiCompatibleChatClient.StreamContentAsync(
                           _settings,
                           messages,
                           remoteMax,
                           _settings.Temperature,
                           cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk))
            {
                yield return chunk;
            }
        }
    }

    private async Task<InterpretationResult> SendAsync(
        string domain,
        string prompt,
        string? systemPrompt,
        string? mode,
        bool structured,
        CancellationToken cancellationToken)
    {
        var messages = structured
            ? new[]
            {
                new ChatTurn("system", systemPrompt ?? ReadingPromptProtocol.SystemPrompt),
                new ChatTurn("user", prompt)
            }
            : new[]
            {
                new ChatTurn("system", $"你是谨慎的{domain}解读助手。盘面和规则摘要由系统计算，不能改动已计算事实。"),
                new ChatTurn("user", prompt)
            };

        try
        {
            using var response = await OpenAiCompatibleChatClient.SendAsync(
                _settings,
                new OpenAiChatRequest(messages, _settings.MaxTokens, _settings.Temperature, JsonOutput: structured),
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new InterpretationResult(
                    string.Empty,
                    true,
                    $"{(int)response.StatusCode}: {OpenAiCompatibleChatClient.Trim(body, 180)}");
            }

            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var text = OpenAiCompatibleChatClient.ExtractMessageContent(doc.RootElement) ?? string.Empty;
            return new InterpretationResult(
                structured ? ReadingPromptProtocol.NormalizeOutput(text) : text,
                false,
                null);
        }
        catch (Exception ex)
        {
            return new InterpretationResult(string.Empty, true, ex.Message);
        }
    }

    private static ReadingPromptPacket SanitizePacket(ReadingPromptPacket packet) =>
        packet with
        {
            Question = PromptInputSanitizer.SanitizeUserText(packet.Question),
            Focus = PromptInputSanitizer.SanitizeUserText(packet.Focus),
            UserQuestion = PromptInputSanitizer.SanitizeUserText(packet.UserQuestion)
        };
}

/// <summary>
/// 商业版：App → 自建 Lab.Api（服务端持有 Key）。
/// </summary>
public sealed class CommercialLabProvider : IInterpretationProvider
{
    private readonly IClientRuntimeSettings _settings;

    public CommercialLabProvider(IClientRuntimeSettings settings) => _settings = settings;

    public string ProviderId => "commercial-lab";
    public bool SupportsFollowUp => true;

    public async Task<InterpretationResult> InterpretAsync(
        InterpretationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsLabConfigured)
        {
            return new InterpretationResult(string.Empty, true, "服务未配置");
        }

        if (request.Tier <= 0)
        {
            return new InterpretationResult(string.Empty, true, "Tier 0 请查看规则摘要");
        }

        if (request.LabReadBody is null)
        {
            return new InterpretationResult(string.Empty, true, "缺少排盘请求体");
        }

        try
        {
            using var doc = await LabApiClient.PostReadAsync(
                _settings.LabApiUrl,
                request.Domain,
                request.Tier,
                request.LabReadBody,
                string.IsNullOrWhiteSpace(_settings.AuthToken) ? null : _settings.AuthToken,
                cancellationToken);
            if (doc is null)
            {
                return new InterpretationResult(string.Empty, true, "服务暂时不可用");
            }

            var text = LabReadResponseParser.TryGetNarrativeText(doc.RootElement);
            if (string.IsNullOrWhiteSpace(text))
            {
                return new InterpretationResult(string.Empty, true, "服务未返回解读");
            }

            var isFallback = LabReadResponseParser.TryGetIsFallback(doc.RootElement);
            return new InterpretationResult(
                ReadingPromptProtocol.NormalizeOutput(text),
                isFallback,
                isFallback ? "服务降级" : null);
        }
        catch (Exception ex)
        {
            return new InterpretationResult(string.Empty, true, ex.Message);
        }
    }

    public async Task<ConnectionTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.IsLabConfigured)
        {
            return new ConnectionTestResult(false, "服务未配置");
        }

        var ok = await LabApiClient.HealthAsync(_settings.LabApiUrl, cancellationToken);
        return ok
            ? new ConnectionTestResult(true, "服务在线")
            : new ConnectionTestResult(false, "服务不可达");
    }

    /// <summary>商业版追问（非流式）：走 Lab <c>/lab/chat</c> followup。</summary>
    public async Task<InterpretationResult> FollowUpAsync(
        string sessionId,
        string userQuestion,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsLabConfigured)
        {
            return new InterpretationResult(string.Empty, true, "服务未配置");
        }

        try
        {
            using var doc = await LabApiClient.FollowUpAsync(
                _settings.LabApiUrl,
                sessionId,
                PromptInputSanitizer.SanitizeUserText(userQuestion) ?? string.Empty,
                maxTokens,
                string.IsNullOrWhiteSpace(_settings.AuthToken) ? null : _settings.AuthToken,
                cancellationToken);
            if (doc is null)
            {
                return new InterpretationResult(string.Empty, true, "追问失败：服务不可用");
            }

            var text = LabReadResponseParser.TryGetNarrativeText(doc.RootElement);
            if (string.IsNullOrWhiteSpace(text))
            {
                return new InterpretationResult(string.Empty, true, "服务未返回追问内容");
            }

            return new InterpretationResult(
                ReadingPromptProtocol.NormalizeOutput(text),
                LabReadResponseParser.TryGetIsFallback(doc.RootElement),
                null);
        }
        catch (Exception ex)
        {
            return new InterpretationResult(string.Empty, true, ex.Message);
        }
    }

    public async IAsyncEnumerable<string> StreamFollowUpAsync(
        IReadOnlyList<ChatTurn> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 商业版追问需要 sessionId，见 FollowUpAsync；此处不走裸 messages。
        await Task.CompletedTask;
        yield break;
    }
}
