# 塔罗前端壳演进 Spec

> 编写模型: Claude Fable 5 (Cursor Agent) · 2026-07-16；Cursor Grok 4.5 · 2026-07-16
> 状态：P1–P3 代码已落地，待人工验证清单勾选后归档
> 前置：三版本骨架已落地（2026-07-15，见 [editions.md](../../editions.md)）

## 1. 现状调研（2026-07-16 核对代码）

### 1.1 已落地

| 项 | 现状 |
|----|------|
| UI 共享库 | `IChing.Tarot.App.Shared` 持有全部 Pages / Views / Services / Resources（Styles、Images、Fonts、Raw） |
| 版本 head | `Tarot.App`(DevShell) / `Tarot.Free` / `Tarot.Byok` / `Tarot.Biz` 各只剩 `MauiProgram` + 平台入口 + AppIcon/Splash |
| 能力注入 | `EditionHost.Capabilities`（静态，`MauiProgram` 启动时写入）；`EditionCapabilities.AllowAiInterpretation` 汇总开关 |
| 版本门控 | `DrawPage.ApplyEditionChrome()`（免费版隐藏 AI 按钮、改空态文案）；`SettingsPage.LoadSettings()`（按 caps 显隐 API/Lab/ONNX 分区） |
| 免费版语义 | 仅抽牌 + 牌面 + 基本牌义（`SpreadBoardLayout` 迷你卡直接渲染 `Meaning`），无任何 AI 入口 |
| 模型下载 | `LocalModelDownloader`（HuggingFace 分文件断点续传 + 本地 `models/` 导入），仅 DevShell 可见 |
| 商业后端 | `CommercialAiBootstrap` 服务端 Key；`LabApiClient.FollowUpAsync`；`EngagementReadingProducer`（服务端有，App 未接） |

### 1.2 调研发现的缺口

按「发布一个版本会先撞上什么」排序：

1. **Byok Key 存储**：`AppSettings.ApiKey` 走 `Preferences`（明文）。计划文档承诺 SecureStorage。
2. **Biz 无生产配置**：`LabApiUrl` 默认 `http://localhost:5000` 写死在 `AppSettings.DefaultLabApiUrl`；商业版不应让用户填地址（`ShowLabUrlSettings=true` 目前是调试便利），需编译期/配置注入正式地址 + 服务不可达的降级文案。
3. **品牌不分版本**：四个 head 共用同一套金色 AppIcon/Splash；商店上架至少要免费/商业两套视觉（角标或色调区分）。
4. **商业版增长面未接**：服务端 `EngagementReadingProducer`（每日一签/追问引导）无 App 入口；`IMonetizationSlot` 只有 NoOp，无 UI 挂载点。
5. **追问轮次 UI**：`ExchangeDialogue.MaxRounds=3` 在服务端限制，`FollowUpChatPage` 无剩余轮次提示。
6. **牌面图 CDN**：默认 jsdelivr GitHub 镜像，国内商业发布不可靠；`CardCdnBaseUrl` 已可配，但无按版本的默认值策略。
7. **免费版设置页残留**：API 状态标签、探索配置路径等 DevShell 痕迹仍可见（不挡发布，观感问题）。
8. **Explore 模块无版本过滤**：`ExploreModuleCatalog` 的 JSON 无 edition 字段；目前全部模块无 AI 依赖所以无实际问题，留扩展位即可。

### 1.3 刻意不做（边界）

- **不引入 DI 容器重构**：`EditionHost` 静态注入 + `App.*` 静态服务够用；改成构造注入是大动干戈无收益。ponytail: 全局静态的天花板是「单窗口单 edition」，MAUI App 本来如此。
- **不接支付/广告 SDK**：`IMonetizationSlot` 维持占位，只补 UI 挂载点。
- **不做 Linux 宿主**、不动 Lab 内核。

## 2. 设计

### 2.1 P1 — 版本可发布性打磨（先做）

| 事项 | 设计 |
|------|------|
| Byok SecureStorage | `AppSettings.ApiKey` 改为 `SecureStorage.Default`（异步 API，包一层带内存缓存的同步读取；平台不支持时回退 Preferences 并在设置页提示）。迁移：首次读取时若 Preferences 有旧值则搬移后删除 |
| Biz 生产地址 | head 工程 `MauiProgram` 写入 `EditionHost.DefaultLabApiUrl`（新增静态属性）；`AppSettings.LabApiUrl` 默认值取它。商业版设置页只读展示服务状态，不给编辑框（`ShowLabUrlSettings` Biz 改回 `false`，DevShell 保持 `true`） |
| Biz 断线态 | `DrawPage` 解读失败且 `Kind==Commercial` 时文案改「服务暂不可用，已展示基本牌义」，不暴露 URL/错误细节 |
| 免费版设置页清理 | `UpdateStatus()` 免费分支已有；再隐藏 API 状态 Border 外壳与无关标签 |

### 2.2 P2 — 品牌与商店

| 事项 | 设计 |
|------|------|
| 分版本图标 | 各 head 的 `Resources/AppIcon/appiconfg.svg` 差异化（免费=银灰、自助=青蓝、商业=金）；`MauiIcon Color` 相应调整。Splash 同色系 |
| 标题/关于 | `EditionHost.DisplayName` 已有；设置页版本行已用它，补免费版「升级引导」文案位（纯文案+商店链接占位，不做内购） |
| 打包核对 | ApplicationId 已分开（`.free` / `.byok` / 商业 `com.iching.tarot`）；核对 Android 签名配置与 WinUI `Package.appxmanifest` 身份不冲突，可同机并装 |

### 2.3 P3 — 商业版增长面

| 事项 | 设计 |
|------|------|
| 每日一签 | Explore 页顶部加「今日提示」卡（仅 `ShowMonetizationSlots=true` 显示）；数据走 `/lab/{domain}/read` 的 engagement domain（服务端 producer 已在），本地按日缓存一条，离线回退固定文案池 |
| 追问轮次提示 | `FollowUpChatPage` 标题栏显示「第 n/3 轮」；达上限禁用输入框并显示引导文案 |
| 广告位挂载点 | `DrawPage` 解读面板下方插入 `MonetizationSlotView`（ContentView，内部问 `IMonetizationSlot.IsEnabled`，NoOp 时高度 0 不可见）；слot 实例由 `EditionHost.MonetizationSlots` 提供 |
| CDN 按版本 | `EditionHost.DefaultCardCdnBase`（可空）；Biz head 注入自有 CDN，其余用现默认 |

### 2.4 P4 — 复制到易占域

塔罗 P1 验证后，把同样的「Shared 库 + 薄 head + EditionHost」模式套到 `IChing.App`（八字/六爻）。单独任务，不在本 spec 展开；入口在 [PENDING.md](../../../PENDING.md)。

### 2.5 Explore 模块 edition 过滤（扩展位，随 P3 顺手）

`ExploreModuleConfig` 加可空 `editions: string[]` 字段；`ExplorePage` 加载后按 `EditionHost.Capabilities.Kind` 过滤，空=全版本可见。JSON 老文件无字段不受影响。

## 3. 验证清单

代码侧（2026-07-16）：Free/Byok/Biz Windows 头工程已成功生成；`EditionScaffoldingTests` 通过。

- [x] P1 代码：Byok `ApiKey` → SecureStorage（失败回退 Preferences + 设置页提示）
- [x] P1 代码：Biz `EditionHost.DefaultLabApiUrl` + `ShowLabUrlSettings=false`；断线友好文案
- [x] P1 代码：Free 设置页按 caps 隐藏 API/Lab/ONNX
- [x] P2 代码：Free 银灰 / Byok 青蓝 / Biz 金（MauiIcon/Splash Color）；设置页升级引导文案
- [x] P3 代码：Explore 今日提示卡；FollowUp `第 n/3 轮`；`MonetizationSlotView`；Explore `editions` 过滤；Biz `ICHING_CARD_CDN`
- [ ] P1 人工：Byok 填 Key → 重启后仍可解读，Preferences 里无明文 Key
- [ ] P1 人工：Biz 断网抽牌 → 基本牌义 + 友好降级，无 localhost 字样
- [ ] P2 人工：三 head 同机并装，图标可肉眼区分
- [ ] P3 人工：Biz Explore 有每日一签；Free/Byok 无
- [ ] P3 人工：追问第 3 轮后输入禁用
- [ ] 回归人工：DevShell 三条 AI 路径；全量 `dotnet test`

## 4. 实施顺序与工作量估计

P1（半天）→ P2（半天，图标素材另计）→ P3（1 天）→ P4（另立任务）。每个 Phase 单独可发布，P1 完成即可出 Byok 首版。
