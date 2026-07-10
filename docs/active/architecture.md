# 架构说明

## 分层

```text
src/
  IChing.Lab.Abstractions/   公共插件和引擎契约
  IChing.Lab.Core/           八字、六爻、塔罗、日历、规则引擎（无 HTML 呈现）
  IChing.Lab.Composition/    Lab DI 装配（ChartEngines + Inference + Plugins）
  IChing.Lab.Presentation/   解读 HTML 呈现（ReadingHtmlFormatter）
  IChing.Lab.Client/         MAUI 用 Lab Tier API 轻量 HTTP 客户端
  BaziEngines/               官方八字排盘引擎
  LiuyaoEngines/             官方六爻排盘引擎
  TarotEngines/              官方塔罗引擎
  CalendarEngines/           官方日历引擎
  IChing.Lab.PluginLoader/   外部插件加载
  IChing.Lab.Inference/      Prompt、ONNX、本地 fallback、推理编排
  IChing.Lab.Api/            HTTP API、Blazor Demo、规则插件管理
    Controllers/LabController.cs   薄路由层（~180 行）
    Contracts/                     API DTO
    Services/                      排盘查询、解读编排、健康探针
  IChing.App/                八字 + 六爻 MAUI App（排盘 Core；解读 Lab API 或远程 LLM）
  IChing.Tarot.App/          塔罗 MAUI App（Lab API 或远程 LLM）
  IChing.Lab.Tests/          测试
```

## 正式与示例

- `src/*Engines` 是正式内置排盘引擎。
- `samples/` 保留外部插件、sidecar、推理引擎样例。
- `plugins/` 是本地构建后的插件投放目录，不提交构建产物。

## 规则引擎

Layer1 规则在 `src/IChing.Lab.Core/Rules`。

当前使用内置插件表：

- 按 domain 选择插件
- `Enabled=false` 跳过
- `Weight < MinWeight` 跳过
- 输出 `ruleDigest.activePlugins` 与 `ruleDigest.items`

API 层提供运行时启停与权重调整，并保存到 `App_Data/rule-engine-options.json`。

## App 策略

- 八字和六爻合并在 `IChing.App`，避免维护两个重复 App。
- 塔罗保留独立 App，因为它有探索页、牌面资源、人格测评和导出等独立体验。
- MAUI App 排盘仍走 Core 静态 API；Tier 1+ 解读优先 `IChing.Lab.Client` 调用 Lab `/lab/{domain}/read`，失败或未配置时降级远程 OpenAI 兼容 HTTP。Follow-up 追问仍走直连 LLM。
- Lab.Api Tier 1+ 可通过 `Accounts:Enabled` 在解读前调用 Accounts `/api/credits/consume`（默认关闭）。
- **AI 交互统一设计**见 [design/reading-exchange.md](design/reading-exchange.md)（`ReadingExchange` 实体、envelope v2、Producer 分层）。
- Lab.Api 通过 `IChing.Lab.Composition` 统一装配引擎、推理与外部插件。
- 旧 WPF `IChing.Desktop` 暂停，默认不参与构建。
