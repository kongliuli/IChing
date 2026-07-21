# 架构说明

> 编写模型: Claude Fable 5 (Cursor Agent) · 2026-07-16

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
  IChing.Lab.PromptTest/     本地提示词/模型试跑（无 HTTP）
  IChing.Lab.Api/            HTTP API、Blazor Demo、规则插件管理
    Controllers/             薄路由层（Lab / Chat / Credits / RulePlugins）
    Contracts/               API DTO
    Services/                排盘查询、解读编排、健康探针
    Commercial/              商业版服务端 Key 装配（CommercialAiBootstrap）
  IChing.Accounts.Api/       用户、额度、Mock 支付
  IChing.Client.Shared/      客户端共享层：IInterpretationProvider、EditionCapabilities、
                             输入清洗、模型下载器、IMonetizationSlot
  IChing.App/                八字 + 六爻 MAUI App（排盘 Core；解读经 Provider）
  IChing.Tarot.App.Shared/   塔罗域 UI 共享库（Pages / Views / Services / Resources）
  IChing.Tarot.App/          塔罗开发壳 head（DevShell，全能力）
  IChing.Tarot.Free/         塔罗免费版 head（无 AI）
  IChing.Tarot.Byok/         塔罗自助版 head（用户自带 Key）
  IChing.Tarot.Biz/          塔罗商业版 head（走自建 Lab）
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
- 塔罗采用「共享 UI 库 + 版本 head」形态：`IChing.Tarot.App.Shared` 持有全部页面与资源，四个 head（DevShell / Free / Byok / Biz）只做能力注入与品牌，见 [editions.md](editions.md)。
- 三版本差异收敛到 `IChing.Client.Shared` 的 `IInterpretationProvider` + `EditionCapabilities`；免费版无 AI，自助版 BYOK，商业版走自建 Lab（Key 在服务端）。
- MAUI App 排盘仍走 Core 静态 API；Tier 1+ 解读经 `CompositeInterpretationProvider` 按版本路由（Lab `/lab/{domain}/read` / BYOK 远端 / 端侧 ONNX / 规则降级）。Follow-up 追问按版本走 Lab chat 或直连 LLM。
- Lab.Api Tier 1+ 可通过 `Accounts:Enabled` 在解读前调用 Accounts `/api/credits/consume`（默认关闭）。
- **AI 交互统一设计**见 [design/reading-exchange.md](design/reading-exchange.md)（`ReadingExchange` 实体、envelope v2、Producer 分层）。
- Lab.Api 通过 `IChing.Lab.Composition` 统一装配引擎、推理与外部插件。
- 旧 WPF `IChing.Desktop` 暂停，默认不参与构建。
- 未实现事项见 [roadmap.md](roadmap.md)。
