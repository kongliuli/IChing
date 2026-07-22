# API 参考

> 编写日期：2026-07-22  
> 本文档列出 Lab.Api 和 Accounts.Api 的全部 HTTP 端点，包含请求/响应结构、curl 示例和 JSON 样例。  
> 代码定义位于 `src/IChing.Lab.Api/Contracts/` 和 `src/IChing.Accounts.Api/Program.cs`。

---

## 目录

- [Lab.Api 端点（端口 5000）](#labapi-端点端口-5000)
  - [排盘端点](#排盘端点)
  - [解读端点](#解读端点)
  - [会话管理](#会话管理)
  - [额度与计费](#额度与计费)
  - [引擎与插件管理](#引擎与插件管理)
  - [健康检查](#健康检查)
  - [已废弃端点](#已废弃端点)
- [Accounts.Api 端点（端口 5002）](#accountsapi-端点端口-5002)
- [ReadingEnvelopeV2 完整结构](#readingenvelopev2-完整结构)

---

## Lab.Api 端点（端口 5000）

### 排盘端点

#### 1. POST /lab/bazi — 八字排盘

**请求体：** `BaziRequest`

```csharp
public record BaziRequest(
    int Year,           // 公历年
    int Month,          // 月
    int Day,            // 日
    int Hour,           // 时
    int Minute = 0,     // 分
    int Second = 0,     // 秒
    double? Longitude = null,  // 经度（用于真太阳时校正）
    string? City = null,       // 城市名
    int? Gender = null,        // 性别：1=男, 2=女
    int Sect = 1,              // 流派：1=默认
    int? FlowYear = null,      // 流年
    int? FlowMonth = null,     // 流月
    int? FlowCalendarMonth = null,  // 流月（农历）
    int? FlowDay = null        // 流日
);
```

**响应体：**

```json
{
  "chart": {
    "yearPillar": { "stem": "甲", "branch": "子", "naYin": "海中金" },
    "monthPillar": { "stem": "丙", "branch": "寅", "naYin": "炉中火" },
    "dayPillar": { "stem": "戊", "branch": "午", "naYin": "天上火" },
    "hourPillar": { "stem": "壬", "branch": "戌", "naYin": "大海水" },
    "dayMaster": "戊",
    "daYun": [...]
  },
  "engine": {
    "paipan": "cnlunar-port"
  }
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5000/lab/bazi \
  -H "Content-Type: application/json" \
  -d '{
    "year": 1990,
    "month": 1,
    "day": 15,
    "hour": 10,
    "minute": 30,
    "gender": 1,
    "sect": 1
  }'
```

---

#### 2. POST /lab/bazi/read?tier={0|1|2} — 八字解读

**请求体：** `BaziReadRequest`（继承 `BaziRequest` 全部字段，额外增加）

```csharp
public record BaziReadRequest(
    // ... BaziRequest 全部字段 ...
    string? Focus = null,       // 关注角度：事业/财运/感情/健康/综合
    int? MaxTokens = null       // 最大生成 token 数
);
```

**响应体：** `ReadingEnvelopeV2`（见[完整结构](#readingenvelopev2-完整结构)）

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/lab/bazi/read?tier=1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "year": 1990,
    "month": 1,
    "day": 15,
    "hour": 10,
    "focus": "事业",
    "maxTokens": 512
  }'
```

---

#### 3. POST /lab/bazi/hepan — 合盘比较

**请求体：** `HePanRequest`

```csharp
public record HePanRequest(
    BaziRequest PersonA,
    BaziRequest PersonB
);
```

**响应体：**

```json
{
  "comparison": {
    "personA": { /* BaziChart */ },
    "personB": { /* BaziChart */ },
    "compatibility": {
      "dayMasterRelation": "相生",
      "score": 75
    }
  },
  "engine": {
    "paipanA": "cnlunar-port",
    "paipanB": "cnlunar-port"
  }
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5000/lab/bazi/hepan \
  -H "Content-Type: application/json" \
  -d '{
    "personA": { "year": 1990, "month": 1, "day": 15, "hour": 10, "gender": 1 },
    "personB": { "year": 1992, "month": 5, "day": 20, "hour": 14, "gender": 2 }
  }'
```

---

#### 4. GET /lab/bazi/cities — 城市经度列表

**响应体：**

```json
[
  { "name": "北京", "longitude": 116.4 },
  { "name": "上海", "longitude": 121.5 },
  { "name": "广州", "longitude": 113.3 }
]
```

**curl 示例：**

```bash
curl http://localhost:5000/lab/bazi/cities
```

---

#### 5. POST /lab/liuyao/coin?seed={N} — 六爻铜钱起卦

**查询参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `seed` | int? | 随机种子（可选，用于复现） |

**响应体：**

```json
{
  "chart": {
    "originalHexagram": "水雷屯",
    "changedHexagram": "泽雷随",
    "changingLineIndexes": [2, 5],
    "shiYaoIndex": 3,
    "yingYaoIndex": 6,
    "lines": [...]
  },
  "engine": {
    "paipan": "iching-library"
  }
}
```

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/lab/liuyao/coin?seed=42"
```

---

#### 6. POST /lab/liuyao/time?at={ISO8601} — 六爻时间起卦

**查询参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `at` | DateTime? | 起卦时间（ISO 8601 格式，默认当前时间） |

**响应体：** 同铜钱起卦

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/lab/liuyao/time?at=2026-07-22T10:30:00Z"
```

---

#### 7. POST /lab/liuyao/read?tier={N} — 六爻解读

**请求体：** `LiuyaoReadRequest`

```csharp
public record LiuyaoReadRequest(
    string? Method,           // "coin" | "time"
    DateTimeOffset? At,       // 起卦时间
    int? Seed,                // 随机种子
    string? Question,         // 问事内容
    string? Focus,            // 关注角度
    int? MaxTokens            // 最大 token 数
);
```

**响应体：** `ReadingEnvelopeV2`

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/lab/liuyao/read?tier=1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "method": "coin",
    "seed": 42,
    "question": "这次投资能否成？",
    "focus": "财运",
    "maxTokens": 512
  }'
```

---

#### 8. POST /lab/tarot/draw — 塔罗抽牌

**请求体：** `TarotDrawRequest`

```csharp
public record TarotDrawRequest(
    string? SpreadId,    // 牌阵 ID：past-present-future / celtic-cross 等
    string? Question,    // 问事内容
    int? Seed            // 随机种子
);
```

**响应体：**

```json
{
  "reading": {
    "spreadId": "past-present-future",
    "question": "Should I change jobs?",
    "positions": [
      {
        "key": "past",
        "title": "过去",
        "card": {
          "name": "The Tower",
          "nameZh": "塔",
          "isReversed": true,
          "meaningEn": "Sudden change, upheaval",
          "meaningZh": "突变、动荡"
        }
      }
    ]
  },
  "engine": {
    "paipan": "tarot-default"
  }
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5000/lab/tarot/draw \
  -H "Content-Type: application/json" \
  -d '{
    "spreadId": "past-present-future",
    "question": "Should I change jobs?",
    "seed": 7
  }'
```

---

#### 9. POST /lab/tarot/read?tier={N} — 塔罗解读

**请求体：** `TarotReadRequest`

```csharp
public record TarotReadRequest(
    string? SpreadId,     // 牌阵 ID
    string? Question,     // 问事内容
    int? Seed,            // 随机种子
    int? MaxTokens        // 最大 token 数
);
```

**响应体：** `ReadingEnvelopeV2`

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/lab/tarot/read?tier=1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "spreadId": "past-present-future",
    "question": "Should I change jobs?",
    "seed": 7,
    "maxTokens": 600
  }'
```

---

#### 10. GET /lab/tarot/spreads — 牌阵列表

**响应体：**

```json
[
  {
    "id": "past-present-future",
    "title": "过去-现在-未来",
    "positionCount": 3,
    "positions": [
      { "key": "past", "title": "过去" },
      { "key": "present", "title": "现在" },
      { "key": "future", "title": "未来" }
    ]
  },
  {
    "id": "celtic-cross",
    "title": "凯尔特十字",
    "positionCount": 10
  }
]
```

**curl 示例：**

```bash
curl http://localhost:5000/lab/tarot/spreads
```

---

#### 11. GET /lab/calendar/day?year={Y}&month={M}&day={D}&sect={N} — 日历日课

**查询参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `year` | int | 公历年 |
| `month` | int | 月 |
| `day` | int | 日 |
| `sect` | int | 流派：1=默认, 2=_alternative |

**响应体：**

```json
{
  "solar": "2026-07-22",
  "lunar": "丙午年 六月 初一",
  "ganZhi": { "year": "丙午", "month": "乙未", "day": "壬子" },
  "wuXing": { "year": "天河水", "month": "沙中金", "day": "桑柘木" },
  "jianChu": "建",
  "yi": ["祭祀", "出行"],
  "ji": ["动土", "开仓"]
}
```

**curl 示例：**

```bash
curl "http://localhost:5000/lab/calendar/day?year=2026&month=7&day=22&sect=1"
```

---

#### 12. POST /lab/{domain}/read?tier={N} — 统一解读入口

**路径参数：**

| 参数 | 值 | 说明 |
|------|-----|------|
| `domain` | `bazi` / `liuyao` / `tarot` | 领域标识 |

**请求体：** 根据 `domain` 自动反序列化为对应的 ReadRequest 类型

**响应体：** `ReadingEnvelopeV2`

**curl 示例：**

```bash
# 八字
curl -X POST "http://localhost:5000/lab/bazi/read?tier=1" \
  -H "Content-Type: application/json" \
  -d '{ "year": 1990, "month": 1, "day": 15, "hour": 10, "focus": "事业" }'

# 六爻
curl -X POST "http://localhost:5000/lab/liuyao/read?tier=1" \
  -H "Content-Type: application/json" \
  -d '{ "method": "coin", "seed": 42, "question": "投资运势" }'

# 塔罗
curl -X POST "http://localhost:5000/lab/tarot/read?tier=1" \
  -H "Content-Type: application/json" \
  -d '{ "spreadId": "past-present-future", "question": "Career change?" }'
```

---

### 会话管理

#### 13. POST /lab/chat — ReadingExchange 会话管理

**请求体：** `LabChatRequest`

```csharp
public sealed record LabChatRequest(
    string Mode,                    // "register" | "initial" | "followup" | "append"
    string? Domain,                 // "bazi" | "liuyao" | "tarot"
    int Tier = 1,                   // 0 | 1 | 2
    string? SessionId,              // 会话 ID（initial/followup/append 必填）
    string? UserQuestion,           // 追问内容（followup 必填）
    string? AssistantReply,         // 追加的助手回复（append 必填）
    int? MaxTokens,                 // 最大 token 数
    ExchangeInput? Input,           // 事实输入（register 可选）
    string? InitialOutput,          // 初始输出文本（register 可选）
    object? Chart,                  // 排盘数据
    BaziReadRequest? Bazi,          // 八字请求（initial 时 domain=bazi）
    LiuyaoReadRequest? Liuyao,      // 六爻请求（initial 时 domain=liuyao）
    TarotReadRequest? Tarot         // 塔罗请求（initial 时 domain=tarot）
);
```

**Mode 说明：**

| Mode | 用途 | 必填字段 | 响应 |
|------|------|----------|------|
| `register` | 创建新会话 | `domain`, `tier` | `{ sessionId, exchangeId }` |
| `initial` | 首次解读 | `sessionId`, `bazi`/`liuyao`/`tarot` | `ReadingEnvelopeV2` |
| `followup` | 追问 | `sessionId`, `userQuestion` | `ReadingEnvelopeV2` |
| `append` | 追加对话 | `sessionId`, `assistantReply` | `{ exchangeId }` |

**响应体（register）：**

```json
{
  "sessionId": "uuid",
  "exchangeId": "uuid"
}
```

**响应体（initial/followup）：** `ReadingEnvelopeV2`

**curl 示例：**

```bash
# 1. 注册会话
curl -X POST http://localhost:5000/lab/chat \
  -H "Content-Type: application/json" \
  -d '{ "mode": "register", "domain": "bazi", "tier": 1 }'
# 返回: { "sessionId": "abc-123", "exchangeId": "ex-001" }

# 2. 首次解读
curl -X POST http://localhost:5000/lab/chat \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "mode": "initial",
    "sessionId": "abc-123",
    "domain": "bazi",
    "tier": 1,
    "bazi": { "year": 1990, "month": 1, "day": 15, "hour": 10, "focus": "事业" }
  }'

# 3. 追问
curl -X POST http://localhost:5000/lab/chat \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "mode": "followup",
    "sessionId": "abc-123",
    "userQuestion": "流年运势如何？"
  }'

# 4. 追加对话
curl -X POST http://localhost:5000/lab/chat \
  -H "Content-Type: application/json" \
  -d '{
    "mode": "append",
    "sessionId": "abc-123",
    "assistantReply": "根据命盘分析..."
  }'
```

---

### 额度与计费

#### 14. POST /lab/credits/consume — 扣费代理

Lab.Api 代理转发到 Accounts.Api，客户端不直连 Accounts。

**请求体：** `LabCreditsConsumeRequest`

```csharp
public sealed record LabCreditsConsumeRequest(
    string ExchangeId,       // 交互 ID
    string? SessionId,       // 会话 ID
    string Domain,           // "bazi" | "liuyao" | "tarot"
    string Mode,             // "initial" | "followup"
    int Tier = 1             // 0 | 1 | 2
);
```

**请求头：**

```
Authorization: Bearer {token}
```

**响应体：**

```json
// 成功
{ "ok": true, "skipped": false, "remainingCredits": 49 }

// Accounts 未启用
{ "ok": true, "skipped": true }

// 额度不足
// HTTP 402
{ "error": "insufficient credits" }

// Token 无效
// HTTP 401
{ "error": "unauthorized" }
```

**curl 示例：**

```bash
curl -X POST http://localhost:5000/lab/credits/consume \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "exchangeId": "ex-001",
    "sessionId": "abc-123",
    "domain": "bazi",
    "mode": "followup",
    "tier": 1
  }'
```

---

### 引擎与插件管理

#### 15. GET /lab/engines — 排盘引擎列表

**响应体：**

```json
[
  {
    "domain": "bazi",
    "engineId": "cnlunar-port",
    "source": "builtin",
    "version": "1.0.0",
    "algorithmBasis": "lunar-csharp-1.6.8 C# port",
    "templateHint": "bazi-tier1-default",
    "moduleFocus": ["yongshen", "geju"]
  },
  {
    "domain": "bazi",
    "engineId": "openfate-bridge",
    "source": "plugin",
    "version": "1.0.0",
    "algorithmBasis": "openfate HTTP sidecar"
  }
]
```

**curl 示例：**

```bash
curl http://localhost:5000/lab/engines
```

---

#### 16. GET /lab/rules/plugins — 规则插件列表

**查询参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `domain` | string? | 过滤领域：bazi / liuyao / tarot |

**响应体：**

```json
[
  {
    "id": "bazi-yongshen-classic",
    "domain": "bazi",
    "name": "经典用神",
    "enabled": true,
    "weight": 500,
    "description": "传统用神取法"
  }
]
```

**curl 示例：**

```bash
curl "http://localhost:5000/lab/rules/plugins?domain=bazi"
```

---

#### 17. PUT /lab/rules/plugins/{id} — 更新规则插件

**路径参数：**

| 参数 | 说明 |
|------|------|
| `id` | 插件 ID |

**请求体：**

```json
{
  "enabled": true,     // 是否启用
  "weight": 500        // 权重：0-1000
}
```

**响应体：**

```json
{
  "id": "bazi-yongshen-classic",
  "enabled": true,
  "weight": 500
}
```

**curl 示例：**

```bash
curl -X PUT http://localhost:5000/lab/rules/plugins/bazi-yongshen-classic \
  -H "Content-Type: application/json" \
  -d '{ "enabled": true, "weight": 800 }'
```

---

#### 18. GET /lab/interpret/status — 推理引擎加载状态

**响应体：**

```json
{
  "loaded": true,
  "engineId": "onnx-genai-qwen2.5-1.5b",
  "modelPath": "./models/qwen2.5-1.5b-genai"
}
```

**curl 示例：**

```bash
curl http://localhost:5000/lab/interpret/status
```

---

### 健康检查

#### 19. GET /health — 存活探活

**响应体：**

```json
{ "status": "healthy" }
```

**curl 示例：**

```bash
curl http://localhost:5000/health
```

---

#### 20. GET /health/engines — 推理引擎健康状态

**响应体：**

```json
{
  "engines": [
    {
      "engineId": "onnx-genai-qwen2.5-1.5b",
      "isReady": true,
      "modelLoaded": true
    },
    {
      "engineId": "template-fallback",
      "isReady": true
    }
  ]
}
```

**curl 示例：**

```bash
curl http://localhost:5000/health/engines
```

---

#### 21. GET /health/chart-engines — 排盘引擎健康状态

**响应体：**

```json
{
  "engines": [
    {
      "domain": "bazi",
      "engineId": "cnlunar-port",
      "isReady": true
    },
    {
      "domain": "liuyao",
      "engineId": "iching-library",
      "isReady": true
    }
  ]
}
```

**curl 示例：**

```bash
curl http://localhost:5000/health/chart-engines
```

---

### 已废弃端点

以下端点仍保留兼容，但不再接新客户端。

#### POST /lab/bazi/interpret → 改用 /lab/bazi/read?tier=1

**状态：** Deprecated，响应带 `Deprecation` 头

**替代：** `POST /lab/bazi/read?tier=1`

---

#### POST /lab/tarot/interpret → 改用 /lab/tarot/read?tier=1

**状态：** Deprecated

**替代：** `POST /lab/tarot/read?tier=1`

---

#### POST /lab/interpret → 410 Gone

**状态：** Gone（410）

**替代：** 使用各域 `/lab/{domain}/read`

---

## Accounts.Api 端点（端口 5002）

Accounts.Api 使用 ASP.NET Core Minimal API，无 Controller 类，所有端点定义在 `Program.cs`。

---

#### 22. POST /api/register — 注册

**请求体：**

```json
{
  "phone": "13800138000",
  "password": "password123",
  "nickname": "张三"
}
```

**响应体：**

```json
{
  "userId": 1,
  "phone": "13800138000",
  "createdAt": "2026-07-22T10:00:00Z"
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5002/api/register \
  -H "Content-Type: application/json" \
  -d '{
    "phone": "13800138000",
    "password": "password123",
    "nickname": "张三"
  }'
```

---

#### 23. POST /api/login — 登录

**请求体：**

```json
{
  "phone": "13800138000",
  "password": "password123"
}
```

**响应体：**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 1,
  "expiresAt": "2026-07-23T10:00:00Z"
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5002/api/login \
  -H "Content-Type: application/json" \
  -d '{
    "phone": "13800138000",
    "password": "password123"
  }'
```

---

#### 24. GET /api/credits — 查额度

**请求头：**

```
Authorization: Bearer {token}
```

**响应体：**

```json
{
  "interpretCredits": 50,
  "usedCredits": 10,
  "memberLevel": "standard"
}
```

**curl 示例：**

```bash
curl http://localhost:5002/api/credits \
  -H "Authorization: Bearer {token}"
```

---

#### 25. POST /api/credits/consume — 扣减额度

**请求体：**

```json
{
  "amount": 1,
  "readingId": "reading-001"
}
```

**说明：** 同一 `readingId` 24 小时内不重复扣费（幂等）。

**响应体：**

```json
// 成功
{
  "ok": true,
  "consumed": 1,
  "remainingCredits": 49
}

// 额度不足
// HTTP 400
{ "error": "insufficient credits" }
```

**curl 示例：**

```bash
curl -X POST http://localhost:5002/api/credits/consume \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{ "amount": 1, "readingId": "reading-001" }'
```

---

#### 26. POST /api/orders/mock-pay — Mock 支付

**请求体：**

```json
{
  "productType": "membership",  // "membership" | "topup"
  "amount": 99.00
}
```

**说明：**

- `membership`：赠 30 次解读额度
- `topup`：赠 10 次解读额度

**响应体：**

```json
{
  "orderId": "order-001",
  "creditsAwarded": 30,
  "totalCredits": 80
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5002/api/orders/mock-pay \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{ "productType": "membership", "amount": 99.00 }'
```

---

#### 27. GET /health — 存活

**响应体：**

```json
{ "status": "healthy" }
```

**curl 示例：**

```bash
curl http://localhost:5002/health
```

---

## ReadingEnvelopeV2 完整结构

所有 `*/read`（tier ≥ 0）端点的统一响应格式。

```json
{
  "schema": "reading-envelope.v2",
  "sessionId": "uuid",
  "exchange": {
    "meta": {
      "schema": "reading-exchange.v1",
      "exchangeId": "uuid",
      "sessionId": "uuid",
      "parentExchangeId": "uuid",
      "domain": "bazi",
      "mode": "initial",
      "tier": 1,
      "language": "zh-CN",
      "createdAt": "2026-07-22T10:00:00Z"
    },
    "input": {
      "question": "事业运势",
      "focus": "事业",
      "computedFacts": [
        "pillars: year=甲子; month=丙寅; day=戊午; hour=壬戌",
        "dayMaster: 戊"
      ],
      "ruleDigest": [
        "用神: 官鬼",
        "日主旺相"
      ],
      "pluginContext": [
        {
          "pluginId": "bazi-yongshen-classic",
          "facts": ["用神取官鬼"],
          "outputSections": ["yongshen"]
        }
      ],
      "chartRef": { /* 排盘数据引用 */ }
    },
    "render": {
      "outputSchema": "reading-output.v2",
      "outputSections": [
        { "key": "overview", "title": "总论" },
        { "key": "yongshen", "title": "用神" },
        { "key": "geju", "title": "格局" },
        { "key": "flow", "title": "大运" },
        { "key": "advice", "title": "建议" }
      ],
      "systemDirectives": [
        "不要修改干支数据"
      ],
      "promptTemplateId": "bazi-tier1-default",
      "promptProfile": "openai-json"
    },
    "dialogue": {
      "history": [
        { "role": "user", "content": "事业运势如何？" },
        { "role": "assistant", "content": "根据命盘分析..." }
      ],
      "userQuestion": "流年运势如何？",
      "maxRounds": 3
    },
    "output": {
      "structured": {
        "schema": "reading-output.v2",
        "summary": "木火偏旺，宜泄秀生财。",
        "sections": [
          {
            "key": "overview",
            "title": "总论",
            "body": "日主戊土生于寅月，得令而旺..."
          },
          {
            "key": "yongshen",
            "title": "用神",
            "body": "用神取官鬼甲木..."
          }
        ],
        "warnings": [],
        "meta": {
          "confidence": "normal",
          "disclaimer": "本解读由 AI 生成，仅供参考"
        }
      },
      "rawText": "完整原始文本...",
      "textEn": "English version (tarot only)",
      "engineId": "onnx-genai-qwen2.5-1.5b",
      "isFallback": false,
      "fallbackReason": null,
      "promptTemplateId": "bazi-tier1-default"
    }
  },
  "chart": {
    /* 域相关的排盘数据 */
  },
  "tier0Preview": {
    "oneLiner": "日主旺相，事业有成。",
    "disclaimer": "概览由规则模板生成，非 AI 解读"
  }
}
```

**字段说明：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `schema` | string | 固定值 `"reading-envelope.v2"` |
| `sessionId` | string | 会话 ID |
| `exchange.meta` | object | 交互元数据 |
| `exchange.input` | object | 事实输入（computedFacts + ruleDigest + pluginContext） |
| `exchange.render` | object | 渲染规格（输出 schema、section 列表、prompt 模板） |
| `exchange.dialogue` | object | 对话历史（追问时存在） |
| `exchange.output` | object | AI 输出（structured + rawText + engineId） |
| `chart` | object | 排盘数据（BaziChart / LiuyaoNajiaResult / TarotReading） |
| `tier0Preview` | object | Tier 0 规则摘要（tier ≥ 1 时也返回，便于 UI 预览） |

---

## 相关文档

- [Lab API](lab-api.md)：路由概览
- [架构说明](architecture.md)：项目分层
- [架构图集](architecture-diagrams.md)：Mermaid 架构图
- [ReadingExchange 设计](design/reading-exchange.md)：统一 AI 交互协议
- [Accounts API](accounts-api.md)：用户与额度
