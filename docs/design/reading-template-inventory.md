# Reading 模板注册表迁移清单

> 对应 ADR：[reading-exchange.md](./reading-exchange.md) §8  
> 实现入口：`IChing.Lab.Core/Readings/Templates/ReadingTemplateRegistry.cs`

## Scriban 文件（`prompts/*.txt`）

| templateId | domain | tier | mode | producerId | 说明 |
|------------|--------|------|------|------------|------|
| `bazi-tier1-default` | bazi | 1 | initial | core.bazi | 全局 default |
| `bazi-tier1-lunar-default` | bazi | 1 | initial | core.bazi | lunar 引擎 variant |
| `bazi-tier1-lunar-yongshen` | bazi | 1 | initial | core.bazi | lunar + yongshen 模块 |
| `bazi-tier1-lunar-geju` | bazi | 1 | initial | core.bazi | lunar + geju 模块 |
| `bazi-tier1-combined` | bazi | 1 | initial | core.bazi | 多模块拼装 |
| `bazi-tier1-cnlunar-default` | bazi | 1 | initial | core.bazi | cnlunar 引擎 |
| `bazi-tier1-openfate-default` | bazi | 1 | initial | core.bazi | openfate 引擎 |
| `liuyao-tier1-default` | liuyao | 1 | initial | core.liuyao | 全局 default |
| `liuyao-tier1-sixlines-najia` | liuyao | 1 | initial | core.liuyao | najia 模块 |
| `liuyao-tier1-sixlines-shensha` | liuyao | 1 | initial | core.liuyao | shensha 模块 |
| `tarot-tier1-en` | tarot | 1 | initial | core.tarot | 英文 + 翻译 pass |
| `tarot-tier1-deckaura-default` | tarot | 1 | initial | core.tarot | 中文直出 |
| `tarot-tier2-celtic-cross` | tarot | 2 | initial | core.tarot | 凯尔特十字长文 |
| `tarot-translate-to-zh` | tarot | 1 | translate | core.tarot | 英译中第二 pass |

## Core 注册（非 Scriban）

| templateId | domain | mode | 来源 | 说明 |
|------------|--------|------|------|------|
| `core-followup-json` | * | followup | `FollowUpPromptBuilder` | 追问 JSON 契约 |
| `reading-template-{domain}-initial` | * | initial | `ReadingPromptTemplateManager` | App Remote JSON packet 默认节 |

## Packet → Adapter 映射

| 路径 | 请求视图 | 输出 schema |
|------|----------|-------------|
| App Remote LLM | `ReadingPromptPacket` (`reading-request.v1`) | `reading-output.v2` |
| Lab Scriban | `PromptContext` + Scriban 模板 | `reading-output.v2`（`ReadingJsonOutputContract` 后缀） |
| 追问 Remote | `FollowUpPromptBuilder` 压缩 JSON | `reading-output.v2` |
| Lab HTTP | `reading-envelope.v2` | `exchange.output.structured` |

## 迁移状态

- [x] `ReadingTemplateRegistry` 元数据（templateId / producerId / translate 标记）
- [x] 塔罗 `(engineId, tier, spreadId)` 解析迁入 Registry
- [x] Lab read 三域 tier≥1 走 Scriban + JSON v2
- [x] Inference `PromptContext.FromExchange(exchange)`（`ExchangePromptAdapter`）
- [x] 追问 `ReadingExchange` + `ExchangePromptAdapter.ToFollowUpPacket`
- [x] Lab `POST /lab/chat`（register / initial / followup）
- [x] Tarot App 追问 Exchange 化（`LocalSessionStore` + `ExchangePromptAdapter`）
