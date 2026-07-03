using System.Diagnostics;
using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;

namespace IChing.Lab.ChartBridge;

/// <summary>
/// MCP 桥接抽象基类：通过 stdio 启动 MCP server 子进程，按 JSON-RPC 调用其 tools/call 获取排盘结果。
/// 子类提供 <see cref="McpServerCommand"/> / <see cref="McpServerArgs"/> / <see cref="McpToolName"/> /
/// <see cref="EngineId"/> / <see cref="Domain"/> / <see cref="Metadata"/>。
/// 桥接只产 chart JSON，不产解读文本；任何异常均以错误对象返回，不抛出。
/// </summary>
public abstract class McpChartBridge : IChartEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>进程启动超时（毫秒），用于等待 server 就绪并完成 initialize 握手。</summary>
    protected virtual int InitializeTimeoutMs => 5000;

    /// <summary>tools/call 单次调用的读超时（毫秒）。</summary>
    protected virtual int CallTimeoutMs => 10000;

    /// <summary>MCP server 启动命令，例如 "npx" / "node" / "python"。</summary>
    protected abstract string McpServerCommand { get; }

    /// <summary>MCP server 启动参数，例如 ["-y", "@mymcp-fun/bazi"]。</summary>
    protected abstract string[] McpServerArgs { get; }

    /// <summary>要调用的 MCP 工具名，例如 "get_bazi_details"。</summary>
    protected abstract string McpToolName { get; }

    /// <summary>引擎标识，在同一领域内唯一区分不同实现。</summary>
    public abstract string EngineId { get; }

    /// <summary>领域标识，例如 bazi / liuyao / tarot。</summary>
    public abstract string Domain { get; }

    /// <summary>引擎元数据，由子类提供。</summary>
    public abstract EngineMetadata Metadata { get; }

    /// <summary>
    /// 启动 MCP server 进程，发送 initialize + tools/call JSON-RPC，解析 result.content[0].text 返回。
    /// 进程启动失败或协议异常返回错误对象，不抛异常。
    /// </summary>
    public object Calculate(ChartRequest request)
    {
        Process? process = null;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = McpServerCommand,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var arg in McpServerArgs)
            {
                startInfo.ArgumentList.Add(arg);
            }

            process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return ErrorResult("mcp process start failed");
            }

            // 1) initialize 握手
            var initResponse = SendJsonRpc(
                process,
                id: 1,
                method: "initialize",
                @params: new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { },
                    clientInfo = new { name = "IChing.Lab.ChartBridge", version = "1.0" }
                },
                InitializeTimeoutMs);
            if (initResponse is null)
            {
                return ErrorResult("mcp initialize timeout or invalid");
            }

            // 发送 initialized 通知（无 id，无响应）
            WriteNotification(process, "notifications/initialized", new { });

            // 2) tools/call
            var callResponse = SendJsonRpc(
                process,
                id: 2,
                method: "tools/call",
                @params: new
                {
                    name = McpToolName,
                    arguments = request.Args
                },
                CallTimeoutMs);
            if (callResponse is null)
            {
                return ErrorResult("mcp tools/call timeout or invalid");
            }

            // 3) 解析 result.content[0].text
            var callRoot = callResponse.RootElement;
            if (!callRoot.TryGetProperty("result", out var resultEl) ||
                !resultEl.TryGetProperty("content", out var contentEl) ||
                contentEl.ValueKind != JsonValueKind.Array ||
                contentEl.GetArrayLength() == 0)
            {
                return new
                {
                    engine = new { paipan = EngineId, ready = true },
                    error = "mcp unexpected response shape",
                    raw = callRoot.Clone()
                };
            }

            var firstContent = contentEl[0];
            if (!firstContent.TryGetProperty("text", out var textEl) ||
                textEl.ValueKind != JsonValueKind.String)
            {
                return new
                {
                    engine = new { paipan = EngineId, ready = true },
                    error = "mcp content[0].text missing",
                    raw = callRoot.Clone()
                };
            }

            var text = textEl.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return new
                {
                    engine = new { paipan = EngineId, ready = true },
                    error = "mcp content[0].text empty"
                };
            }

            // text 通常是 JSON 字符串，尝试解析为对象；解析失败则原样返回字符串。
            try
            {
                using var textDoc = JsonDocument.Parse(text);
                return textDoc.RootElement.Clone();
            }
            catch
            {
                return text;
            }
        }
        catch (Exception ex)
        {
            return new
            {
                engine = new { paipan = EngineId, ready = false },
                error = "mcp unavailable",
                detail = ex.Message
            };
        }
        finally
        {
            try { process?.Kill(); } catch { /* 忽略清理异常 */ }
            process?.Dispose();
        }
    }

    /// <summary>构造统一错误对象。</summary>
    private object ErrorResult(string message) => new
    {
        engine = new { paipan = EngineId, ready = false },
        error = "mcp unavailable",
        detail = message
    };

    /// <summary>发送 JSON-RPC 请求（带 id），读取一行响应并解析为 JsonDocument；超时或异常返回 null。</summary>
    private JsonDocument? SendJsonRpc(Process process, int id, string method, object @params, int timeoutMs)
    {
        try
        {
            var payload = new
            {
                jsonrpc = "2.0",
                id,
                method,
                @params
            };
            var line = JsonSerializer.Serialize(payload, JsonOptions);
            process.StandardInput.WriteLine(line);
            process.StandardInput.Flush();

            var readTask = process.StandardOutput.ReadLineAsync();
            if (!readTask.Wait(timeoutMs))
            {
                return null;
            }

            var responseLine = readTask.GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(responseLine))
            {
                return null;
            }

            return JsonDocument.Parse(responseLine);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>发送 JSON-RPC 通知（无 id，不读响应）。</summary>
    private void WriteNotification(Process process, string method, object @params)
    {
        try
        {
            var payload = new
            {
                jsonrpc = "2.0",
                method,
                @params
            };
            var line = JsonSerializer.Serialize(payload, JsonOptions);
            process.StandardInput.WriteLine(line);
            process.StandardInput.Flush();
        }
        catch
        {
            // 通知失败不影响后续 tools/call，忽略。
        }
    }
}
