# 未实现路线图

> **状态**：active 清单（2026-07-15 从各设计文档梳理）
> **说明**：已落地能力见 [reading-template-inventory.md](./design/reading-template-inventory.md)、[archive/specs](../archive/specs/README.md)。本页只列**尚未实现**或仅部分实现的事项。

---

## 产品与计费

| 事项 | 来源 | 现状 |
|------|------|------|
| 额度推广刷新（邀请、签到、活动码） | [inference-layer-design.md](./inference-layer-design.md) §2.5、Phase 3 | 未做；Accounts 仅有 mock-pay 赠次 |
| Accounts 持久化存储 | [accounts-api.md](./accounts-api.md) | 进程内内存；生产表结构需重新设计 |
| Tier 1+ 同一 `readingId` 24h 免重复扣费（生产级） | inference-layer-design §2.5 | Accounts consume 有 readingId 去重，推广额度表 / Redis 未建 |
| 娱乐 Mode `EntitlementGate`（会员/每日次数，不按 exchange 扣费） | [reading-exchange.md](./design/reading-exchange.md) | 测评现无 AI、无扣费；远期门控未实现 |

## 解读质量与数据

| 事项 | 来源 | 现状 |
|------|------|------|
| 小阿卡纳英文牌库 Phase A（Waite 完整牌义） | inference-layer-design Phase 4；[research-tarot-optimization.md](../archive/research/research-tarot-optimization.md) | 未导入完整英文牌库 |
| 八字 Layer1 深化 + Tier 2 分段详析 | inference-layer-design Phase 5 | Tier1 与部分规则已有；Tier2 多段未齐 |
| 六爻健康类用神定派（子孙 vs 官鬼） | inference-layer-design §4.1 | 产品未最终定一派 |
| 感情用神二期分性别 | inference-layer-design §4.1 | 未做 |

## ReadingExchange 远期

| 事项 | 来源 | 现状 |
|------|------|------|
| PromptProfile 扩展 | reading-exchange 决策 #5 | 现不纠结；未扩展 |
| AI summary / 用户人格 memory | reading-exchange §追问压缩 | 暂不做；仍用规则截断 |
| 流派独立 pack 目录 | reading-exchange；[reading-plugin-extensions.md](./design/reading-plugin-extensions.md) | 首期不做；仍用 RuleEngine 开关 |
| Lab `/lab/chat` 增强（可选 Phase 6） | reading-exchange 分期 | 基础 chat 已落地；可选增强未排期 |

## 平台与发布

| 事项 | 来源 | 现状 |
|------|------|------|
| 塔罗三版本发布打磨（SecureStorage、品牌、增长面） | [specs/tarot-shell-evolution/spec.md](./specs/tarot-shell-evolution/spec.md) | 骨架已落地，按 spec P1→P3 推进 |
| 易占域壳提取（IChing.App.Shared + 版本 head） | [editions.md](./editions.md)、[PENDING](../PENDING.md) | 未开始；等塔罗壳 P1 验证后复制 |
| 端侧 Qwen3.5 GenAI 包接入 | [editions.md](./editions.md) | 无可用 GenAI 导出；下载器已就绪（DevShell） |
| Linux 桌面宿主 | [design/README.md](./design/README.md) | MAUI 覆盖 Android/iOS/MacCatalyst/Windows；Linux 需单独宿主 |
| Accounts 正式支付（非 mock） | accounts-api | 仅 `mock-pay` |

---

## 刻意不做 / 已放弃

| 事项 | 说明 |
|------|------|
| Java / Spring MVP API | 已删除；见 [legacy-java-spike.md](../archive/history/legacy-java-spike.md) |
| `IChing.Desktop` 新功能 | 已暂停；见 [deprecate-desktop](../archive/specs/deprecate-desktop/spec.md) |
| 插件化主线 specs | 已全部完成并归档至 [archive/specs](../archive/specs/README.md) |
