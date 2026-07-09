# ADR：ReadingExchange 统一 AI 交互与结果产出

**状态**：已接受（讨论闭环，待 Phase 0 实现）  
**日期**：2026-07-09  
**替代/补充**：[inference-layer-design.md](../inference-layer-design.md)、[architecture.md](../architecture.md)

---

## 标题与一句话

**ReadingExchange：以独立交互实体统一占卜 AI 的输入、输出、模板与会话，并分离语义解读与确定性 UI 产出。**

---

## 背景与问题

当前 IChing 存在 **三套并行 AI 路径**，输入/输出不统一：

| 路径 | 输入 | 输出 |
|------|------|------|
| App Remote | `ReadingPromptPacket`（reading-request.v1） | `ReadingOutput` → Markdown |
| Lab Inference | `PromptContext` → Scriban ChatML | 自由文本 / 松散 narrative |
| Follow-up | 手工 `(SystemPrompt, Context)` | 流式纯文本 |

追问与首次解读协议分裂；Lab 与 Remote 的 prompt 不同源；结果展示（HTML/widgets）与 AI 语义混杂。

---

## 决策摘要（18 条）

| # | 议题 | 决定 |
|---|------|------|
| 1 | 根实体 | 独立 **`ReadingExchange`**（非扩展 Packet 为双向） |
| 2 | 事实源 | **`computedFacts + ruleDigest + pluginContext`** 为 canonical；`ChartRef` 可选 |
| 3 | Session | App 本地 **SQLite**；FIFO **10** 个 session |
| 4 | AI 输出 | **全路径 JSON** → `reading-output.v2` |
| 5 | PromptProfile | 后续扩展，现不纠结 |
| 6 | 模板 | **`IReadingTemplateRegistry` 归 Core** |
| 7 | HTTP | **H1 破坏性**：`reading-envelope.v2` |
| 8 | 核心扣费 | **G1**：每个 `exchangeId` 扣一次（含追问） |
| 9 | AI schema | **不含 `widgets[]`** |
| 10 | Producer | **全自动** widgets（AI 不参与布局） |
| 11 | 追问压缩 | L1 facts  verbatim + 规则截断 initial JSON + rolling history |
| 12–16 | 娱乐 | **现无 AI、不扣费**；远期 AI **免费** + `EntitlementGate`（会员/条件） |
| 14 | 追问扣费 | **Lab.Api 代理** Accounts；App 不直连 consume |
| 15 | 存储 | SQLite：`iching-sessions.db` |
| 17 | 追问轮数 | **固定 3 轮** |
| 18 | section key | **domain 白名单**；未知 key 仍展示 + `warnings[]` |
| — | 追问推理 | **始终 Remote** OpenAI 兼容；Lab 仅 initial read + credits 代理 |
| — | 流派/六爻 | **RuleEngine 配置开关**；首期不做 pack 目录 |
| — | Initial summary | **暂不做 AI summary**；待人格/memory 体系 |

---

## 核心概念

### ReadingExchange（根实体）

一次与 AI 的完整交互单元（可序列化、可持久化、可回放）。

```text
ReadingExchange
├── Meta      ExchangeMeta     schema, exchangeId, sessionId, parentExchangeId,
│                              domain, mode, tier, language, createdAt
├── Input     ExchangeInput    question, focus, computedFacts[], ruleDigest[],
│                              pluginContext[], chartRef?
├── Dialogue  ExchangeDialogue? history[], userQuestion?, maxRounds=3
├── Render    ExchangeRenderSpec  outputSchema, outputSections[], systemDirectives[],
│                                  promptTemplateId, promptProfile
└── Output    ExchangeOutput?  structured (ReadingOutput v2), rawText, engineId,
                               isFallback, fallbackReason, promptTemplateId
```

**Mode 枚举**：`initial` | `followup` | `translate` | `tier0` | `entertainment`（远期）

### ReadingSession

```text
ReadingSession
├── sessionId, domain, tier, createdAt
├── chartJson          完整 chart 快照（SQLite）
├── exchanges[]        按时间序的 exchangeId 链
└── FIFO               全局最多 10 个 session
```

### ReadingPromptPacket（Legacy 传输视图）

定义于 `IChing.Lab.Core/Readings/ReadingPromptProtocol.cs`。

- **现状**：App Remote 将 Packet 序列化为 OpenAI user JSON（reading-request.v1）
- **演进**：保留为 **`OpenAiJsonAdapter` 的输出 DTO**，不再作为第二事实源
- **工厂**：`ReadingPromptPackets.*Initial` 逻辑迁入 `ReadingExchangeFactory`

### 结果产出三层

```text
AI JSON (reading-output.v2)
  → ReadingOutputParser
  → IReadingResultProducer（全自动 Widgets）
  → ReadingViewModel
  → IReadingPresenter（MAUI / HTML）
```

- **AI 负责**：`summary`、`sections[]`、`warnings[]`
- **Producer 负责**：牌阵表、四柱盘、卦象线、测评条形图等 **Widgets**（不进 AI schema）

---

## Schema 契约

### reading-output.v2（AI 必须遵守）

```json
{
  "schema": "reading-output.v2",
  "summary": "string",
  "sections": [
    { "key": "overview", "title": "string", "body": "string" }
  ],
  "warnings": ["string"],
  "meta": {
    "confidence": "normal",
    "disclaimer": "string"
  }
}
```

**刻意不含 `widgets[]`。**

#### Section key 白名单（示例）

| domain | 允许 key（示例） |
|--------|------------------|
| bazi | overview, yongshen, geju, flow, advice |
| liuyao | overview, yongshen, changing, advice |
| tarot | overview, spread, advice + 各 position key |

未知 key：Parser 仍渲染，`warnings` 追加 `unknown_section_key:{key}`。

### reading-envelope.v2（Lab HTTP 响应，H1）

```json
{
  "schema": "reading-envelope.v2",
  "sessionId": "uuid",
  "exchange": { },
  "chart": { },
  "tier0Preview": { "oneLiner": "...", "disclaimer": "..." }
}
```

删除旧 `narrative.text` 松散形状。

### POST /lab/credits/consume（追问扣费代理）

App 在 Remote follow-up **之前**调用：

```json
{
  "exchangeId": "uuid",
  "sessionId": "uuid",
  "domain": "bazi",
  "mode": "followup"
}
```

Header: `Authorization: Bearer {token}`  
Lab 内部复用 `AccountsCreditsGateway` → Accounts `/api/credits/consume`。

---

## 对话流

### Initial（核心解读）

```text
本地 Core 排盘
  → ReadingExchangeFactory.CreateInitial
  → [Lab read | Remote JSON] 按 UseLabApi 选择
  → ReadingOutputParser
  → IReadingResultProducer
  → UI / HTML
  → SQLite 写入 session + exchange
```

### Follow-up（3 轮，Remote + Lab 代理扣费）

```text
LoadSession(SQLite)
  → CreateFollowUp(exchange, userQuestion)
  → ExchangeContextCompactor（L1 facts + 规则截断 initial + rolling 1–2 轮）
  → POST /lab/credits/consume（G1）
  → Remote StreamAsync（core-followup-json adapter）
  → Parse JSON → Producer → UI
  → Append exchange；round >= 3 禁用输入
```

**不经过 Lab 推理**；Lab 仅代理额度。

### 娱乐测评（现阶段）

```text
规则计分（PersonalityQuizScorer 等）
  → ProducerId=entertainment.quiz.*
  → 无 ReadingExchange / 无 AI / 无 Accounts consume
```

远期：`Mode=entertainment` + `EntitlementGate`（会员/每日次数），**不按 exchange 扣费**。

---

## 模板体系（Core 统一注册）

**`IReadingTemplateRegistry`** 索引：

```text
templateId, domain, mode, tier, profile, sourceKind, sourcePath,
requiredOutputSchema, producerId, extensionPluginIds[]
```

| templateId | mode | 说明 |
|------------|------|------|
| bazi-tier1-default 等 | initial | Scriban `prompts/*.txt` |
| core-followup-json | followup | 替代 FollowUpPromptTemplates |
| tarot-translate-to-zh | translate | 塔罗第二 pass（独立 Exchange） |
| tier0-preview | tier0 | 无 AI |

**Inference 层**：仅 `PromptContext.FromExchange(exchange)` adapter，不维护第二套事实。

---

## 领域扩展

| 类型 | 机制 | 示例 |
|------|------|------|
| 塔罗牌阵 | **抽牌规则** | `SpreadCatalog` → `TarotDrawPipeline` |
| 八字流派 / 六爻断法 | **RuleEngine 插件开关** | `RuleEngine:Plugins.{id}.Enabled` |
| AI 上下文追加 | **PromptExtensions** | SystemDirectives + OutputSections 随插件启用 |

流派 **不改 chart**，只改 `Exchange.Render` 与 `OutputSections`。

---

## 追问上下文压缩（首期）

| 层 | 内容 |
|----|------|
| L1 Hot | computedFacts + ruleDigest + pluginContext（immutable） |
| L2 | initial 的 `reading-output.v2`；超 token 预算时 **规则截断**（非 AI summary） |
| L3 | 最近 1–2 轮 follow-up Q/A |

**暂不做** AI summary pass；待用户人格模型 / 历次提问 memory 再引入。

---

## 与现有代码映射

| 现有 | 演进 |
|------|------|
| `ReadingPromptPacket` | OpenAiJsonAdapter 视图 |
| `ReadingPromptPackets.*` | `ReadingExchangeFactory` |
| `FollowUpPromptTemplates` | `core-followup-json` + Exchange |
| `FollowUpChatPage` | Session + Lab consume + Remote |
| `LabReadService.ReadEnvelope` | `reading-envelope.v2` |
| `Orchestrator.Interpret()` legacy | 废弃 → `bazi-tier1-default` |
| `HtmlReadingTemplate` / `ReadingHtmlFormatter` | `IReadingPresenter` |

---

## 实施分期

| Phase | 内容 |
|-------|------|
| 0 | Abstractions 类型 + Parser + Envelope v2 类型 |
| 1 | Core Template Registry + Lab bazi 去 legacy + 全路径 JSON prompts |
| 2 | SQLite Session + Follow-up Exchange + `/lab/credits/consume` + Compactor |
| 3 | IReadingResultProducer + 三域 Presenter |
| 4 | RuleEngine 流派 PromptExtensions |
| 5 | 娱乐 Producer（无 AI）；远期 EntitlementGate |
| 6（可选） | Lab `/lab/chat` |

---

## JSON 样例：八字 initial（节选）

```json
{
  "meta": {
    "schema": "reading-exchange.v1",
    "exchangeId": "ex-001",
    "sessionId": "ses-001",
    "domain": "bazi",
    "mode": "initial",
    "tier": 1,
    "language": "zh-CN"
  },
  "input": {
    "focus": "事业",
    "computedFacts": ["pillars: year=甲子; ...", "dayMaster: 甲"],
    "ruleDigest": ["用神: ..."],
    "pluginContext": []
  },
  "render": {
    "outputSchema": "reading-output.v2",
    "promptTemplateId": "bazi-tier1-default",
    "promptProfile": "openai-json"
  },
  "output": {
    "structured": {
      "schema": "reading-output.v2",
      "summary": "木火偏旺，宜泄秀生财。",
      "sections": [
        { "key": "overview", "title": "总论", "body": "..." },
        { "key": "yongshen", "title": "用神", "body": "..." }
      ],
      "warnings": []
    },
    "engineId": "deepseek-remote",
    "isFallback": false
  }
}
```

---

## 相关文档

- [architecture.md](../architecture.md) — 分层总览
- [inference-layer-design.md](../inference-layer-design.md) — Tier 与额度策略（本 ADR 细化 G1/娱乐 EntitlementGate）
- [accounts-api.md](../accounts-api.md) — Accounts consume
- [lab-api.md](../lab-api.md) — Lab HTTP（待更新 envelope v2）

---

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-07-09 | 初版：讨论闭环，18 条决策固化 |
