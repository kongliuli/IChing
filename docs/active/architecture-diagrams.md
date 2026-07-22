# 架构图集

> 编写日期：2026-07-22  
> 本文档使用 Mermaid 绘制 IChing 项目的核心架构，所有图表均基于代码库实际结构。

---

## 1. 总体分层架构图

展示 25 个项目在 4 层中的分布。Web/API 层提供 HTTP 接口，核心服务层负责编排与装配，领域引擎层实现排盘算法，客户端层承载 MAUI 应用。

```mermaid
graph TB
    subgraph "Web/API 层"
        LabApi[IChing.Lab.Api<br/>端口 5000]
        AccountsApi[IChing.Accounts.Api<br/>端口 5002]
    end

    subgraph "核心服务层"
        Inference[IChing.Lab.Inference<br/>推理编排]
        PluginLoader[IChing.Lab.PluginLoader<br/>插件加载]
        Composition[IChing.Lab.Composition<br/>DI 装配]
        Abstractions[IChing.Lab.Abstractions<br/>公共契约]
        Core[IChing.Lab.Core<br/>领域引擎基座]
        Presentation[IChing.Lab.Presentation<br/>HTML 呈现]
    end

    subgraph "领域引擎层"
        BaziEngines[BaziEngines<br/>八字排盘]
        LiuyaoEngines[LiuyaoEngines<br/>六爻排盘]
        TarotEngines[TarotEngines<br/>塔罗引擎]
        CalendarEngines[CalendarEngines<br/>日历引擎]
    end

    subgraph "客户端层"
        App[IChing.App<br/>八字+六爻]
        TarotApp[IChing.Tarot.App<br/>开发壳]
        TarotFree[IChing.Tarot.Free<br/>免费版]
        TarotByok[IChing.Tarot.Byok<br/>自助版]
        TarotBiz[IChing.Tarot.Biz<br/>商业版]
        TarotAppShared[IChing.Tarot.App.Shared<br/>UI 共享库]
        ClientShared[IChing.Client.Shared<br/>客户端共享]
        LabClient[IChing.Lab.Client<br/>Lab HTTP 客户端]
    end

    LabApi --> Inference
    LabApi --> Core
    LabApi --> AccountsApi
    AccountsApi --> LabApi
    
    Inference --> Abstractions
    Core --> Abstractions
    Composition --> Inference
    Composition --> Core
    Composition --> PluginLoader
    
    PluginLoader --> BaziEngines
    PluginLoader --> LiuyaoEngines
    PluginLoader --> TarotEngines
    PluginLoader --> CalendarEngines
    
    BaziEngines --> Core
    LiuyaoEngines --> Core
    TarotEngines --> Core
    CalendarEngines --> Core
    
    App --> ClientShared
    App --> LabClient
    App --> Core
    
    TarotApp --> TarotAppShared
    TarotFree --> TarotAppShared
    TarotByok --> TarotAppShared
    TarotBiz --> TarotAppShared
    
    TarotAppShared --> ClientShared
    TarotAppShared --> LabClient
    
    ClientShared --> LabClient
```

---

## 2. AI 推理降级链流程图

`ChartInterpretationOrchestrator` 按配置的降级链依次尝试推理引擎。当前代码库仅实现了 ONNX 和模板兜底两个引擎，降级链通过 `plugins:fallbackChain` 配置。每个引擎失败时记录原因并继续下一个，全部失败时由 `TemplateFallbackEngine` 兜底，保证永不抛异常。

```mermaid
graph TD
    Start[开始推理] --> CheckChain{读取降级链配置}
    CheckChain -->|plugins:fallbackChain| Engine1[OnnxGenAiEngine<br/>onnx-genai-qwen2.5-1.5b]
    
    Engine1 -->|成功| Success[返回结果<br/>IsFallback=false]
    Engine1 -->|失败: model not loaded / inference error| Engine2[TemplateFallbackEngine<br/>template-fallback]
    
    Engine2 -->|始终成功| Fallback[返回模板结果<br/>IsFallback=true]
    
    style Engine1 fill:#e1f5ff
    style Engine2 fill:#fff4e1
    style Success fill:#d4edda
    style Fallback fill:#fff3cd
```

**引擎失败条件：**

| 引擎 | EngineId | 失败条件 | 备注 |
|------|----------|----------|------|
| OnnxGenAiEngine | `onnx-genai-qwen2.5-1.5b` | 模型目录不存在、`genai_config.json` 缺失、推理异常、取消 | 懒加载，首次调用时加载模型 |
| TemplateFallbackEngine | `template-fallback` | 永不失败 | `IsReady` 始终为 `true`，返回固定模板文本 |

**降级链配置示例（appsettings.json）：**

```json
{
  "plugins": {
    "fallbackChain": [
      "onnx-genai-qwen2.5-1.5b",
      "template-fallback"
    ]
  }
}
```

---

## 3. 排盘引擎降级链

每个域（bazi/liuyao/tarot/calendar）注册 6 个引擎：1 个内置 + 5 个插件模块提供。`ChartEngineRouter` 按配置的引擎链依次调用，返回错误结果时自动尝试下一个。

```mermaid
graph LR
    subgraph "Bazi 域"
        BaziBuiltIn[BaziChartEngine<br/>内置]
        BaziCnlunar[BaziCnlunarPortEngine<br/>C# 移植]
        BaziOpenfate[BaziOpenfateBridgeEngine<br/>HTTP 桥接]
        BaziAlvamind[BaziAlvamindBridgeEngine<br/>HTTP 桥接]
        BaziLunarPython[BaziLunarPythonBridgeEngine<br/>HTTP 桥接]
        BaziMymcp[BaziMymcpBridgeEngine<br/>MCP 桥接]
    end
    
    subgraph "Liuyao 域"
        LiuyaoBuiltIn[LiuyaoChartEngine<br/>内置]
        LiuyaoPlugins[5 个插件引擎<br/>LiuyaoEnginesModule]
    end
    
    subgraph "Tarot 域"
        TarotBuiltIn[TarotChartEngine<br/>内置]
        TarotPlugins[5 个插件引擎<br/>TarotEnginesModule]
    end
    
    subgraph "Calendar 域"
        CalendarBuiltIn[CalendarEngine<br/>内置]
        CalendarPlugins[5 个插件引擎<br/>CalendarEnginesModule]
    end
    
    Router[ChartEngineRouter] --> BaziBuiltIn
    Router --> BaziCnlunar
    Router --> BaziOpenfate
    Router --> BaziAlvamind
    Router --> BaziLunarPython
    Router --> BaziMymcp
    
    Router --> LiuyaoBuiltIn
    Router --> LiuyaoPlugins
    
    Router --> TarotBuiltIn
    Router --> TarotPlugins
    
    Router --> CalendarBuiltIn
    Router --> CalendarPlugins
```

**引擎注册模式：**

```csharp
// BaziEnginesModule.Register()
public void Register(IServiceCollection services)
{
    services.AddSingleton<IChartEngine, BaziCnlunarPortEngine>();
    services.AddSingleton<IChartEngine, BaziOpenfateBridgeEngine>();
    services.AddSingleton<IChartEngine, BaziAlvamindBridgeEngine>();
    services.AddSingleton<IChartEngine, BaziLunarPythonBridgeEngine>();
    services.AddSingleton<IChartEngine, BaziMymcpBridgeEngine>();
}
```

**IChartEngine 接口：**

```csharp
public interface IChartEngine
{
    string Domain { get; }              // 领域标识：bazi/liuyao/tarot/calendar
    string EngineId { get; }            // 引擎唯一标识
    EngineMetadata Metadata { get; }    // 算法来源、版本、依据
    object Calculate(ChartRequest request);  // 执行排盘
}
```

**路由配置（appsettings.json）：**

```json
{
  "plugins": {
    "chartEngines": [
      {
        "domain": "bazi",
        "default": "cnlunar-port",
        "fallback": ["openfate-bridge", "alvamind-bridge"]
      }
    ]
  }
}
```

---

## 4. 3-Tier 解读模型流程图

三域（八字/六爻/塔罗）共用同一套 Tier 语义。Tier 0 仅规则引擎，Tier 1 单次 AI 生成，Tier 2 多段生成（塔罗走英译中两 pass）。

```mermaid
graph TD
    subgraph "Tier 0: 规则摘要"
        T0_Start[请求 tier=0] --> RuleEngine[RuleEngine<br/>Layer1 规则]
        RuleEngine --> RuleDigest[ruleDigest<br/>用神/旬空/旺衰]
        RuleDigest --> T0_Preview[Tier0Preview<br/>oneLiner + disclaimer]
        T0_Preview --> T0_End[返回 envelope v2<br/>无 AI 调用]
    end
    
    subgraph "Tier 1: 单次 AI 解读"
        T1_Start[请求 tier=1] --> Chart[排盘结果]
        Chart --> Input[ExchangeInput<br/>computedFacts + ruleDigest]
        Input --> Orchestrator[InferenceOrchestrator]
        Orchestrator --> PromptBuilder[PromptBuilder<br/>bazi-tier1-default]
        PromptBuilder --> AI[AI 引擎<br/>ONNX/Remote]
        AI --> Exchange[ReadingExchange<br/>mode=initial]
        Exchange --> Envelope[reading-envelope.v2]
    end
    
    subgraph "Tier 2: 双通道 AI 解读"
        T2_Start[请求 tier=2] --> T2_Chart[排盘结果]
        T2_Chart --> T2_Input[ExchangeInput]
        T2_Input --> T2_Orchestrator[InferenceOrchestrator]
        T2_Orchestrator --> T2_Pass1[Pass 1: 英文推理<br/>tarot-tier1-en]
        T2_Pass1 --> T2_TextEn[textEn]
        T2_TextEn --> T2_Pass2[Pass 2: 翻译成中文<br/>tarot-translate-to-zh]
        T2_Pass2 --> T2_Merge[合并输出<br/>textZh + textEn]
        T2_Merge --> T2_Envelope[reading-envelope.v2]
    end
    
    style T0_End fill:#d4edda
    style Envelope fill:#d4edda
    style T2_Envelope fill:#d4edda
```

**Tier 对照表：**

| Tier | 名称 | 费用 | AI 调用 | 典型篇幅 | 内容构成 |
|------|------|------|---------|----------|----------|
| 0 | 概览 Overview | 免费 | 否 | 50-150 字 | 结构化字段 + 规则模板 one-liner |
| 1 | 简析 Brief | 会员/次数 | 单次 | 200-400 字 | Layer1 结论 + 单次生成 |
| 2 | 详析 Deep | 高阶付费 | 多段 | 800-1500 字 | Layer1 全量 + 多段拼接（塔罗英译中） |

---

## 5. Lab Chat 会话流程图

`POST /lab/chat` 实现 ReadingExchange 协议，支持四种模式：register（创建会话）、initial（首次解读）、followup（追问）、append（追加对话）。会话存储在内存 `ConcurrentDictionary`，最多 100 个，FIFO 淘汰。

```mermaid
sequenceDiagram
    participant Client as 客户端
    participant LabApi as Lab.Api
    participant ChatService as LabChatService
    participant ReadService as LabReadService
    participant Orchestrator as InferenceOrchestrator
    
    Note over Client,Orchestrator: 1. 注册会话
    Client->>LabApi: POST /lab/chat<br/>{ mode: "register", domain: "bazi", tier: 1 }
    LabApi->>ChatService: RegisterSessionAsync()
    ChatService-->>LabApi: sessionId
    LabApi-->>Client: 200 { sessionId, exchangeId }
    
    Note over Client,Orchestrator: 2. 首次解读
    Client->>LabApi: POST /lab/chat<br/>{ mode: "initial", sessionId, bazi: {...} }
    LabApi->>ReadService: ExecuteBaziReadAsync()
    ReadService->>Orchestrator: GenerateWithFallbackAsync()
    Orchestrator-->>ReadService: GenerationResult
    ReadService-->>LabApi: ReadingEnvelopeV2
    LabApi-->>Client: 200 { schema: "reading-envelope.v2", exchange, chart }
    
    Note over Client,Orchestrator: 3. 追问（最多 3 轮）
    Client->>LabApi: POST /lab/chat<br/>{ mode: "followup", sessionId, userQuestion: "..." }
    LabApi->>ChatService: FollowUpAsync()
    ChatService->>Orchestrator: GenerateWithFallbackAsync()
    Orchestrator-->>ChatService: GenerationResult
    ChatService-->>LabApi: ReadingEnvelopeV2
    LabApi-->>Client: 200 { exchange.output.structured }
    
    Note over Client,Orchestrator: 4. 追加对话历史
    Client->>LabApi: POST /lab/chat<br/>{ mode: "append", sessionId, assistantReply: "..." }
    LabApi->>ChatService: AppendDialogueAsync()
    ChatService-->>LabApi: newExchangeId
    LabApi-->>Client: 200 { exchangeId }
```

**LabChatRequest 结构：**

```csharp
public sealed record LabChatRequest(
    string Mode,                    // "register" | "initial" | "followup" | "append"
    string? Domain,                 // "bazi" | "liuyao" | "tarot"
    int Tier,                       // 0 | 1 | 2
    string? SessionId,              // 会话 ID
    string? UserQuestion,           // 追问内容
    string? AssistantReply,         // 追加的助手回复
    int? MaxTokens,                 // 最大 token 数
    ExchangeInput? Input,           // 事实输入
    string? InitialOutput,          // 初始输出文本
    object? Chart,                  // 排盘数据
    BaziReadRequest? Bazi,          // 八字请求
    LiuyaoReadRequest? Liuyao,      // 六爻请求
    TarotReadRequest? Tarot         // 塔罗请求
);
```

---

## 6. Accounts API + Lab Credits 鉴权流程图

Lab.Api 在 Tier ≥ `Accounts:RequireForTierGte`（默认 1）时，解读前调用 Accounts.Api 扣减额度。客户端通过 `Authorization: Bearer` 头传递 JWT，Lab.Api 转发给 Accounts.Api 验证。同一 `readingId` 24 小时内不重复扣费。

```mermaid
sequenceDiagram
    participant Client as 客户端
    participant LabApi as Lab.Api
    participant Gateway as AccountsCreditsGateway
    participant AccountsApi as Accounts.Api
    participant Store as AccountStore
    
    Client->>LabApi: POST /lab/bazi/read?tier=1<br/>Authorization: Bearer {token}
    LabApi->>Gateway: TryConsumeAsync(auth, tier, readingId)
    
    alt Accounts:Enabled = false 或 tier < MinTier
        Gateway-->>LabApi: Skipped（不扣费）
    else 需要扣费
        Gateway->>AccountsApi: POST /api/credits/consume<br/>{ amount: 1, readingId }<br/>Authorization: Bearer {token}
        AccountsApi->>Store: TryConsumeCredits(userId, amount, readingId)
        
        alt readingId 24h 内已扣
            Store-->>AccountsApi: true（幂等，不重复扣）
        else 额度充足
            Store->>Store: 扣减 1 credit
            Store->>Store: 缓存 readingId 24h
            Store-->>AccountsApi: true
        else 额度不足
            Store-->>AccountsApi: false
        end
        
        alt 扣费成功
            AccountsApi-->>Gateway: 200 { consumed: 1, interpretCredits: remaining }
            Gateway-->>LabApi: Allowed
            LabApi->>LabApi: 执行解读
            LabApi-->>Client: 200 ReadingEnvelopeV2
        else 额度不足
            AccountsApi-->>Gateway: 402 { error: "insufficient credits" }
            Gateway-->>LabApi: InsufficientCredits
            LabApi-->>Client: 402 { error: "..." }
        else Token 无效
            AccountsApi-->>Gateway: 401 Unauthorized
            Gateway-->>LabApi: Unauthorized
            LabApi-->>Client: 401 { error: "..." }
        end
    end
```

**配置（appsettings.json）：**

```json
{
  "Accounts": {
    "Enabled": true,
    "BaseUrl": "http://localhost:5002",
    "RequireForTierGte": 1
  }
}
```

**AccountsCreditsGateway 逻辑：**

```csharp
public async Task<AccountsConsumeResult> TryConsumeAsync(
    string? authorizationHeader, int tier, string? readingId, CancellationToken ct)
{
    if (!IsEnabled || tier < MinTier) 
        return AccountsConsumeResult.Skipped;
    
    if (string.IsNullOrWhiteSpace(authorizationHeader))
        return AccountsConsumeResult.Unauthorized("missing token");
    
    // POST to Accounts.Api /api/credits/consume
    // Forward Authorization header
    // Return Allowed / Unauthorized / InsufficientCredits
}
```

---

## 7. MAUI App 架构图

八字和六爻合并在 `IChing.App`，塔罗采用「共享 UI 库 + 版本 head」模式。四个塔罗 head（DevShell/Free/Byok/Biz）共享 `IChing.Tarot.App.Shared` 的页面、视图、服务和资源。三版本差异收敛到 `IInterpretationProvider` 和 `EditionCapabilities`。

```mermaid
graph TB
    subgraph "八字+六爻 App"
        BaziLiuyaoApp[IChing.App<br/>MAUI App]
    end
    
    subgraph "塔罗 App 家族"
        TarotApp[IChing.Tarot.App<br/>开发壳 DevShell]
        TarotFree[IChing.Tarot.Free<br/>免费版]
        TarotByok[IChing.Tarot.Byok<br/>自助版 BYOK]
        TarotBiz[IChing.Tarot.Biz<br/>商业版]
    end
    
    subgraph "共享 UI 库"
        TarotShared[IChing.Tarot.App.Shared]
        Pages[Pages/<br/>抽牌/解读/追问/历史]
        Views[Views/<br/>牌阵/卦象/组件]
        Services[Services/<br/>业务逻辑]
        Resources[Resources/<br/>样式/图标/字符串]
    end
    
    subgraph "客户端共享层"
        ClientShared[IChing.Client.Shared]
        IInterpProvider[IInterpretationProvider<br/>解读提供者接口]
        EditionCaps[EditionCapabilities<br/>版本能力标识]
        InputSanitizer[输入清洗]
        ModelDownloader[模型下载器]
        Monetization[IMonetizationSlot<br/>计费插槽]
    end
    
    subgraph "Lab HTTP 客户端"
        LabClient[IChing.Lab.Client]
        LabApiClient[LabApiClient<br/>HTTP 客户端]
        ResponseParser[LabReadResponseParser<br/>envelope v2 解析]
        SessionBridge[ReadingSessionBridge<br/>会话桥接]
    end
    
    subgraph "解读提供者实现"
        CompositeProvider[CompositeInterpretationProvider<br/>按版本路由]
        LocalOnnx[LocalOnnxProvider<br/>端侧 ONNX]
        RuleProvider[RuleOnlyProvider<br/>Tier 0 规则]
        RemoteProvider[RemoteLabProvider<br/>Lab API]
        ByokProvider[ByokRemoteProvider<br/>用户自带 Key]
    end
    
    BaziLiuyaoApp --> ClientShared
    BaziLiuyaoApp --> LabClient
    
    TarotApp --> TarotShared
    TarotFree --> TarotShared
    TarotByok --> TarotShared
    TarotBiz --> TarotShared
    
    TarotShared --> Pages
    TarotShared --> Views
    TarotShared --> Services
    TarotShared --> Resources
    
    TarotShared --> ClientShared
    TarotShared --> LabClient
    
    ClientShared --> IInterpProvider
    ClientShared --> EditionCaps
    ClientShared --> InputSanitizer
    ClientShared --> ModelDownloader
    ClientShared --> Monetization
    
    LabClient --> LabApiClient
    LabClient --> ResponseParser
    LabClient --> SessionBridge
    
    IInterpProvider --> CompositeProvider
    CompositeProvider --> LocalOnnx
    CompositeProvider --> RuleProvider
    CompositeProvider --> RemoteProvider
    CompositeProvider --> ByokProvider
    
    RemoteProvider --> LabApiClient
    
    style TarotShared fill:#e1f5ff
    style ClientShared fill:#fff4e1
    style LabClient fill:#f0e1ff
```

**三版本差异：**

| 版本 | Head 项目 | AI 能力 | 解读路径 | 计费 |
|------|-----------|---------|----------|------|
| 免费版 | `IChing.Tarot.Free` | 仅 Tier 0 | RuleOnlyProvider | 无 |
| 自助版 | `IChing.Tarot.Byok` | Tier 0/1/2 | ByokRemoteProvider（用户 Key） | 无（用户自负） |
| 商业版 | `IChing.Tarot.Biz` | Tier 0/1/2 | RemoteLabProvider（Lab API） | Accounts 扣费 |

**IInterpretationProvider 接口：**

```csharp
public interface IInterpretationProvider
{
    Task<InterpretationResult> InterpretAsync(
        string domain, 
        object chart, 
        InterpretationOptions options, 
        CancellationToken ct);
}
```

**EditionCapabilities 标识：**

```csharp
public sealed record EditionCapabilities(
    bool SupportsAi,              // 是否支持 AI 解读
    bool SupportsByok,            // 是否支持用户自带 Key
    bool SupportsLabApi,          // 是否走 Lab API
    bool RequiresCredits,         // 是否需要额度
    int MaxFollowUpRounds       // 最大追问轮数
);
```

---

## 相关文档

- [架构说明](architecture.md)：项目分层、目录边界
- [Lab API](lab-api.md)：HTTP 路由、envelope v2
- [ReadingExchange 设计](design/reading-exchange.md)：统一 AI 交互协议
- [推理层设计](inference-layer-design.md)：Tier、模型、降级策略
- [API 参考](api-reference.md)：完整端点文档
