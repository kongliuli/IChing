# ChartBridge

排盘算法 HTTP / MCP 桥接基类库。提供两个抽象基类，供各域（bazi / liuyao / tarot / calendar）将外部排盘库包装为 `IChartEngine` 实现。

- `ExternalHttpChartBridge`：通过 HTTP sidecar 调用外部排盘库（如 Node.js / Python 包装的 lunar-python、cnlunar、openfate 等）。
- `McpChartBridge`：通过 stdio 启动 MCP server 进程，调用其 `tools/call` 获取排盘结果。

桥接只产 chart JSON，不产解读文本，不接 LLM/RAG 层。

**本地联调样板**：[`samples/sidecars/IChing.ChartSidecar`](../sidecars/IChing.ChartSidecar/README.md) — `scripts/run-chart-sidecar.cmd`

## Sidecar HTTP 协议

### 探活

```
GET {root}/health
```

- 期望返回 `200 OK`（任意 2xx 均视为就绪）。
- 响应体无强制要求。
- 任何异常或非 2xx 响应均视为未就绪，`Calculate` 返回错误对象（不抛异常）。

### 排盘

```
POST {SidecarUrl}
Content-Type: application/json
```

请求体：

```json
{
  "args": { "...": "..." }
}
```

- `args`：领域特定的输入参数字典，与 `ChartRequest.Args` 同构。键名/值类型由各域 sidecar 自行约定（通常与内置 `BaziInput` / `LiuyaoInput` / `TarotInput` / `CalendarInput` 字段对齐，使用 camelCase）。

响应体：

- 各域 chart JSON，结构由 sidecar 决定（建议与内置 `BaziChart` / `LiuyaoNajiaResult` / `TarotReading` / `HuangLiDay` 字段对齐）。
- 桥接基类以 `JsonDocument` 解析后原样返回 `JsonElement`，不做字段重映射。

### 错误响应

sidecar 不可达、探活失败、HTTP 非 2xx、或解析异常时，`Calculate` 返回如下对象（不抛异常）：

```json
{
  "engine": { "paipan": "<EngineId>", "ready": false },
  "error": "sidecar unavailable",
  "detail": "<可选异常消息>"
}
```

## MCP 桥接协议

`McpChartBridge` 通过 stdio 启动子进程作为 MCP server，按 JSON-RPC 顺序发送：

1. `initialize` —— 握手并声明客户端能力。
2. `tools/call` —— 调用子类提供的 `McpToolName`，参数取 `ChartRequest.Args`。
3. 解析 `result.content[0].text`，原样返回 JSON。

进程启动失败、initialize 超时、tools/call 失败时返回错误对象：

```json
{
  "engine": { "paipan": "<EngineId>", "ready": false },
  "error": "mcp unavailable",
  "detail": "<可选异常消息>"
}
```

## 子类实现示例

```csharp
public sealed class BaziOpenfateBridge : ExternalHttpChartBridge
{
    protected override string SidecarUrl => "http://localhost:5001/bazi";
    public override string Domain => "bazi";
    public override string EngineId => "bazi-openfate-bridge";
    public override EngineMetadata Metadata => new(
        Source: "@openfate/bazi-engine",
        Version: "0.1.0",
        AlgorithmBasis: "openfate bazi-engine npm",
        TemplateHint: "openfate",
        ModuleFocus: ["geju", "yongshen"]);
}
```
