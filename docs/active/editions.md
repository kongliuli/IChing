# 三版本产品线

> 编写模型: Claude Fable 5 (Cursor Agent) · 2026-07-16
> 状态：骨架已落地（2026-07-15）
> 原则：Lab 内核不重写；版本差异收敛到 `IInterpretationProvider`。

## 版本

| 版本 | ApplicationId 示例 | 能力 |
|------|-------------------|------|
| 免费版 | `com.iching.tarot.free` | 本地抽牌 + 牌面 + 基本牌义；**无 AI 解读**、无 Key、无追问 |
| 自助版 | `com.iching.tarot.byok` | BYOK 远端 API + ProviderPresets；可追问 |
| 商业版 | `com.iching.tarot` | App → Lab.Api（服务端 Key）+ 追问 + engagement；广告/支付仅 `IMonetizationSlot` 占位 |

三版本**不递进**，可独立发布或不发布。

## 关键类型

- [`IChing.Client.Shared`](../../src/IChing.Client.Shared/)：Provider / 清洗 / Monetization
- [`IChing.Tarot.App.Shared`](../../src/IChing.Tarot.App.Shared/)：完整塔罗 UI（AppShell / Draw / Explore / Settings / 资源）
- 版本 head 只留 `MauiProgram`（注入 `EditionHost.Capabilities`）+ 平台入口 + 图标：
  - [`IChing.Tarot.Free`](../../src/IChing.Tarot.Free/)
  - [`IChing.Tarot.Byok`](../../src/IChing.Tarot.Byok/)
  - [`IChing.Tarot.Biz`](../../src/IChing.Tarot.Biz/)
  - [`IChing.Tarot.App`](../../src/IChing.Tarot.App/)（DevShell）

## 平台与端侧模型

- **免费版**：不提供 AI / ONNX；抽牌后展示牌面与基本牌义即可
- **自助 / 商业 / DevShell**：按版本走 BYOK 或 Lab；DevShell 仍可测端侧 ONNX
- **可下载包**（非免费版）：`qwen2.5-1.5b-genai`（HuggingFace / 本地 `models/` 导入）
- **iOS**：无端侧 ORT GenAI；免费版同样是纯牌义

## 商业后端

- `CommercialAi` + `CommercialAiBootstrap`：Key 仅服务端
- 通道：`/lab/{domain}/read` + `/lab/chat`
- `EngagementReadingProducer` / `IMonetizationSlot` 占位

## 后续

演进计划（打磨 → 品牌 → 商业增长面 → 复制易占域）见 [specs/tarot-shell-evolution/spec.md](specs/tarot-shell-evolution/spec.md)；任务入口见 [PENDING](../PENDING.md)。
