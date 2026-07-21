# Lab API

运行：

```bat
scripts\run-lab-api.cmd
```

默认入口：

- `/demo`：三域 Web Demo
- `/rules/plugins`：Layer1 规则插件管理页
- `/swagger`：接口文档（以运行时为准）

## 主要路由

| Method | Route | 用途 | 备注 |
| --- | --- | --- | --- |
| POST | `/lab/bazi` | 八字排盘 | |
| POST | `/lab/bazi/read?tier=0` | 八字规则摘要 / Tier 解读 | 返回 [envelope v2](#reading-envelope-v2) |
| POST | `/lab/{domain}/read` | 通用域解读入口 | domain=`bazi`/`liuyao`/`tarot` |
| POST | `/lab/bazi/hepan` | 双人合盘 | |
| GET | `/lab/bazi/cities` | 城市经度 | |
| POST | `/lab/liuyao/coin` | 六爻铜钱起卦 | |
| POST | `/lab/liuyao/time` | 六爻时间起卦 | |
| POST | `/lab/liuyao/read?tier=0` | 六爻规则摘要 / Tier 解读 | envelope v2 |
| POST | `/lab/tarot/draw` | 塔罗抽牌 | |
| POST | `/lab/tarot/read?tier=0` | 塔罗规则摘要 / Tier 解读 | envelope v2 |
| GET | `/lab/tarot/spreads` | 牌阵列表 | |
| GET | `/lab/calendar/day` | 日历日课 | |
| POST | `/lab/chat` | ReadingExchange 会话（register / initial / followup / append） | 见 [reading-exchange.md](./design/reading-exchange.md) |
| POST | `/lab/credits/consume` | 追问扣费代理（转发 Accounts） | Header: Bearer |
| GET | `/lab/engines` | 排盘引擎列表 | |
| GET | `/lab/rules/plugins` | 规则插件状态 | |
| PUT | `/lab/rules/plugins/{id}` | 启停或调权重 | |
| GET | `/lab/interpret/status` | 推理模型加载状态 | |
| GET | `/health` | 存活探活 | |
| GET | `/health/chart-engines` | 排盘引擎健康状态 | |
| GET | `/health/engines` | 推理引擎健康状态 | |

### 已废弃（勿再接新客户端）

| Method | Route | 替代 |
| --- | --- | --- |
| POST | `/lab/bazi/interpret` | `POST /lab/bazi/read?tier=1`（仍返回兼容包装，带 Deprecation 头） |
| POST | `/lab/tarot/interpret` | `POST /lab/tarot/read?tier=1` |
| POST | `/lab/interpret` | **410 Gone**；改用各域 `read` |

## reading-envelope.v2

所有 `*/read`（tier≥0）的正式响应形状：

```json
{
  "schema": "reading-envelope.v2",
  "sessionId": "uuid",
  "exchange": { },
  "chart": { },
  "tier0Preview": { "oneLiner": "...", "disclaimer": "..." }
}
```

- `exchange.output.structured` 为 `reading-output.v2`（`summary` / `sections[]` / `warnings[]`）。
- 完整契约与追问流见 [ReadingExchange ADR](./design/reading-exchange.md)。

## 规则插件配置

规则插件运行时修改会保存到：

```text
src/IChing.Lab.Api/App_Data/rule-engine-options.json
```

服务重启后会优先读取该文件；没有文件时使用 `appsettings.json`。
