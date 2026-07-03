using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;

namespace IChing.Lab.ChartBridge;

/// <summary>
/// HTTP 桥接抽象基类：将 IChartEngine 的 Calculate 调用转发到外部 sidecar HTTP 服务。
/// 子类提供 <see cref="SidecarUrl"/> / <see cref="EngineId"/> / <see cref="Metadata"/> / <see cref="Domain"/>，
/// 基类负责探活与 POST 请求并解析响应 JSON。
/// 桥接只产 chart JSON，不产解读文本；任何异常均以错误对象返回，不抛出。
/// </summary>
public abstract class ExternalHttpChartBridge : IChartEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>用于实际 HTTP 调用的客户端，protected 以便子类（或测试）注入。</summary>
    protected HttpClient HttpClient { get; }

    /// <summary>
    /// 构造桥接。未提供 <paramref name="httpClient"/> 时使用默认 <see cref="HttpClient"/>。
    /// </summary>
    protected ExternalHttpChartBridge(HttpClient? httpClient = null)
    {
        HttpClient = httpClient ?? new HttpClient();
    }

    /// <summary>sidecar 排盘端点 URL，例如 "http://localhost:5001/bazi"。</summary>
    protected abstract string SidecarUrl { get; }

    /// <summary>引擎标识，在同一领域内唯一区分不同实现。</summary>
    public abstract string EngineId { get; }

    /// <summary>领域标识，例如 bazi / liuyao / tarot。</summary>
    public abstract string Domain { get; }

    /// <summary>引擎元数据，由子类提供。</summary>
    public abstract EngineMetadata Metadata { get; }

    /// <summary>
    /// 调用 sidecar 排盘：先 GET {root}/health 探活，再 POST 请求体到 <see cref="SidecarUrl"/>，
    /// 解析响应 JSON 返回。任何异常或非 2xx 响应均返回含 error 的对象，不抛异常。
    /// </summary>
    public object Calculate(ChartRequest request)
    {
        if (!ProbeHealth())
        {
            return new
            {
                engine = new { paipan = EngineId, ready = false },
                error = "sidecar unavailable"
            };
        }

        try
        {
            using var content = new StringContent(
                JsonSerializer.Serialize(new { args = request.Args }, JsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");

            using var response = HttpClient.PostAsync(SidecarUrl, content, CancellationToken.None).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return new
                {
                    engine = new { paipan = EngineId, ready = true },
                    error = $"sidecar status {(int)response.StatusCode}"
                };
            }

            using var stream = response.Content.ReadAsStreamAsync(CancellationToken.None).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(stream);
            return doc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            return new
            {
                engine = new { paipan = EngineId, ready = false },
                error = "sidecar unavailable",
                detail = ex.Message
            };
        }
    }

    /// <summary>
    /// 探活：GET {SidecarUrl 根}/health，2xx 视为就绪。
    /// 任何异常或非 2xx 返回 false，不抛异常。
    /// </summary>
    private bool ProbeHealth()
    {
        try
        {
            var root = ExtractRoot(SidecarUrl);
            var healthUrl = root + "/health";
            using var response = HttpClient.GetAsync(healthUrl, CancellationToken.None).GetAwaiter().GetResult();
            return (int)response.StatusCode >= 200 && (int)response.StatusCode < 300;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>从 sidecar URL 提取 scheme://host[:port] 根部分。</summary>
    private static string ExtractRoot(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.GetLeftPart(UriPartial.Authority);
        }

        // 容错：找不到 scheme 时返回原始字符串（探活大概率失败，由调用方返回错误对象）。
        return url;
    }
}
