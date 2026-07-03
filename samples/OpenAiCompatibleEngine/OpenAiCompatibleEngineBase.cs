using System.Diagnostics;
using System.Text;
using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;

namespace OpenAiCompatibleEngine;

/// <summary>
/// OpenAI 兼容接口（/v1/chat/completions）解读引擎抽象基类。
/// 模式 B（本地 HTTP：Ollama / llama-server）与模式 C（远程 API：OpenAI / Azure OpenAI）共用本类，
/// 子类只需提供 <see cref="Client"/>（含 BaseAddress 与鉴权头）、<see cref="ModelName"/>、<see cref="ApiEndpoint"/>
/// 以及 <see cref="ProbeIsReady"/> 探活逻辑即可。
/// </summary>
public abstract class OpenAiCompatibleEngineBase : IInferenceEngine
{
    /// <summary>已配置好 BaseAddress 与鉴权头的 HTTP 客户端，由子类构造并持有。</summary>
    protected abstract HttpClient Client { get; }

    /// <summary>请求体中使用的模型名（如 qwen2.5:7b / gpt-4o-mini / Azure deployment 名）。</summary>
    protected abstract string ModelName { get; }

    /// <summary>
    /// 聊天补全接口的相对路径（相对 <see cref="Client"/> 的 BaseAddress）。
    /// 如 "/v1/chat/completions" 或 Azure 的 "/openai/deployments/{model}/chat/completions?api-version=..."。
    /// 以 "/" 开头时将相对 BaseAddress 的 host 根解析，自动避免 /v1 重复。
    /// </summary>
    protected abstract string ApiEndpoint { get; }

    /// <inheritdoc />
    public abstract string EngineId { get; }

    // IsReady 缓存字段，避免每次访问都触发探活 HTTP 调用。
    private readonly object _readyLock = new();
    private bool _cachedReady;
    private DateTime _readyExpiresAt = DateTime.MinValue;
    private bool _hasCachedReady;

    /// <summary>
    /// 探活方法，由子类实现。例如本地服务 GET /api/tags 或 /v1/models 返回 200 即就绪。
    /// 远程 API 通常不主动探活，可直接返回 true。
    /// </summary>
    protected abstract bool ProbeIsReady(CancellationToken ct);

    /// <inheritdoc />
    public bool IsReady
    {
        get
        {
            // 命中缓存且未过期则直接返回，避免高频探活。
            lock (_readyLock)
            {
                if (_hasCachedReady && DateTime.UtcNow < _readyExpiresAt)
                {
                    return _cachedReady;
                }
            }

            bool ready;
            try
            {
                ready = ProbeIsReady(CancellationToken.None);
            }
            catch
            {
                // 探活抛异常视为未就绪，避免向上层传播。
                ready = false;
            }

            lock (_readyLock)
            {
                _cachedReady = ready;
                _readyExpiresAt = DateTime.UtcNow.AddSeconds(30);
                _hasCachedReady = true;
                return ready;
            }
        }
    }

    /// <inheritdoc />
    public async Task<GenerationResult> GenerateAsync(string prompt, GenerateOptions options, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // 解析 ChatML 提示为 messages 数组，无标记则整体作为 user 消息。
            var messages = ParseChatMl(prompt);
            var payload = BuildPayload(messages, options);

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await Client.PostAsync(ApiEndpoint, content, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                sw.Stop();
                // HTTP 5xx 视为服务端不可用，标记本引擎未就绪以便降级链切换。
                if ((int)response.StatusCode >= 500)
                {
                    MarkNotReady();
                }
                return new GenerationResult(
                    EngineId,
                    Text: "",
                    IsFallback: true,
                    FallbackReason: $"http error: {(int)response.StatusCode} {response.StatusCode}: {Truncate(body)}",
                    ElapsedMs: sw.ElapsedMilliseconds);
            }

            var text = ParseChoiceContent(body);
            sw.Stop();
            return new GenerationResult(
                EngineId,
                Text: text,
                IsFallback: false,
                FallbackReason: null,
                ElapsedMs: sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // 调用方主动取消，向上传播而非降级。
            sw.Stop();
            throw;
        }
        catch (OperationCanceledException ex)
        {
            // HttpClient 超时会以 TaskCanceledException/OperationCanceledException 形式抛出。
            sw.Stop();
            MarkNotReady();
            return new GenerationResult(
                EngineId,
                Text: "",
                IsFallback: true,
                FallbackReason: $"http error: timeout: {ex.Message}",
                ElapsedMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new GenerationResult(
                EngineId,
                Text: "",
                IsFallback: true,
                FallbackReason: $"http error: {ex.Message}",
                ElapsedMs: sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 将 ChatML 格式的提示拆分为 messages 数组。识别 <c>&lt;|im_start|&gt;role\n...&lt;|im_end|&gt;</c>
    /// 标记按 role 切分；若不含任何 ChatML 标记，则将整段提示作为单条 user 消息。
    /// </summary>
    private static List<ChatMessage> ParseChatMl(string prompt)
    {
        var messages = new List<ChatMessage>();
        const string startMarker = "<|im_start|>";
        const string endMarker = "<|im_end|>";

        if (prompt.IndexOf(startMarker, StringComparison.Ordinal) < 0)
        {
            // 无 ChatML 标记：整体作为 user 消息。
            messages.Add(new ChatMessage("user", prompt));
            return messages;
        }

        // 按 <|im_start|> 切分，每段首行为 role，至 <|im_end|> 之间为 content。
        var segments = prompt.Split(startMarker, StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var endIdx = segment.IndexOf(endMarker, StringComparison.Ordinal);
            var body = endIdx >= 0 ? segment.Substring(0, endIdx) : segment;

            var nlIdx = body.IndexOf('\n');
            string role;
            string content;
            if (nlIdx >= 0)
            {
                role = body.Substring(0, nlIdx).Trim();
                content = body.Substring(nlIdx + 1).Trim();
            }
            else
            {
                role = body.Trim();
                content = string.Empty;
            }

            if (!string.IsNullOrEmpty(role))
            {
                messages.Add(new ChatMessage(role, content));
            }
        }

        // 兜底：解析后仍无消息则整体作为 user 消息。
        if (messages.Count == 0)
        {
            messages.Add(new ChatMessage("user", prompt));
        }

        return messages;
    }

    /// <summary>
    /// 构造 OpenAI 兼容请求体 JSON：model / messages / max_tokens / temperature / top_p / stream=false。
    /// 仅在选项提供值时写入 temperature 与 top_p，避免发送 null。top_k 不属于 OpenAI 接口字段，不写入。
    /// </summary>
    private string BuildPayload(List<ChatMessage> messages, GenerateOptions options)
    {
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WriteString("model", ModelName);

            writer.WriteStartArray("messages");
            foreach (var m in messages)
            {
                writer.WriteStartObject();
                writer.WriteString("role", m.Role);
                writer.WriteString("content", m.Content);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteNumber("max_tokens", options.MaxTokens);
            if (options.Temperature.HasValue)
            {
                writer.WriteNumber("temperature", options.Temperature.Value);
            }
            if (options.TopP.HasValue)
            {
                writer.WriteNumber("top_p", options.TopP.Value);
            }
            writer.WriteBoolean("stream", false);

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    /// <summary>解析响应体 choices[0].message.content；结构缺失时返回空串。</summary>
    private static string ParseChoiceContent(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentEl))
                {
                    return contentEl.GetString() ?? string.Empty;
                }
            }
        }
        catch
        {
            // 解析失败由调用方按降级处理。
        }

        return string.Empty;
    }

    /// <summary>标记本引擎未就绪并刷新缓存有效期，使后续 IsReady 探活尽快重新评估。</summary>
    private void MarkNotReady()
    {
        lock (_readyLock)
        {
            _cachedReady = false;
            _readyExpiresAt = DateTime.UtcNow.AddSeconds(30);
            _hasCachedReady = true;
        }
    }

    /// <summary>截断过长的错误响应体，避免 FallbackReason 过大。</summary>
    private static string Truncate(string? text, int max = 500)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        return text.Length <= max ? text : text.Substring(0, max) + "...";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>释放托管资源（HttpClient）。子类若有额外资源可重写并调用基类实现。</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Client?.Dispose();
        }
    }

    /// <summary>内部消息记录，仅用于构造请求体。</summary>
    protected sealed class ChatMessage
    {
        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public string Role { get; }
        public string Content { get; }
    }
}
