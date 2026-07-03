# 排盘算法插件扩展 Spec（每域 5+ 引擎）

## Why

当前每域仅 1 个内置 `IChartEngine`（bazi/liuyao/tarot/calendar 各 1），无法替换或对照。用户要求：**联网调研保证算法有现实依据，每域扩展到 5+ 种插件**。由于命理算法生态以 Python/Node/MCP 为主、纯 C# 库稀缺，需引入「桥接插件」模式以低成本包装外部库，再辅以 C# 移植，使每域可选引擎数 ≥ 5。

## What Changes

### 1. 桥接插件基础设施（共享）

- 定义 `ExternalHttpChartBridge` 抽象基类（位于 `IChing.Lab.Core` 或新 `samples/ChartBridge`）：通过 HTTP sidecar 调用外部 Python/Node 算法服务，统一 `ChartRequest → JSON → IChartEngine.Calculate` 适配
- 定义 `McpChartBridge` 抽象基类：通过 MCP 协议调用 MCP 服务器（如 mymcp-fun/bazi、tarot-mcp）
- `IChartEngine` 接口扩展 `EngineMetadata` 只读属性（来源库名、版本、算法依据、模块面向），用于审计与 Prompt 联动（见 [algorithm-aware-prompt-templates](file:///workspace/.trae/specs/algorithm-aware-prompt-templates/spec.md)）

### 2. 每域扩展至 5+ 引擎

#### Bazi（目标 6）
| # | EngineId | 类型 | 现实依据 |
|---|----------|------|----------|
| 1 | `bazi-lunar-csharp` | 内置（已有） | [lunar-csharp](https://github.com/6tail/lunar-csharp) 6tail，MIT，0001–9999 年 |
| 2 | `bazi-cnlunar-port` | C# 移植 | [cnlunar](https://pypi.org/project/cnlunar/) 0.2.4，基于《钦定协纪辨方书》，宜忌等第更严谨 |
| 3 | `bazi-openfate-bridge` | HTTP 桥接 | [@openfate/bazi-engine](https://www.npmjs.com/package/@openfate/bazi-engine) 1.1.1，真太阳时+节气+大运+交互检测 |
| 4 | `bazi-alvamind-bridge` | HTTP 桥接 | [bazi-calculator-by-alvamind](https://www.npmjs.com/package/bazi-calculator-by-alvamind) 1.0.2，含八宅/贵人/文昌 |
| 5 | `bazi-lunar-python-bridge` | HTTP 桥接 | [lunar-python](https://github.com/6tail/lunar-python) 6tail Python 版，藏干/十神/纳音 |
| 6 | `bazi-mymcp-bridge` | MCP 桥接 | [@mymcp-fun/bazi](https://lobehub.com/mcp/mymcp-fun-bazi) 2.0.2，MCP 协议八字服务 |

#### Liuyao（目标 6）
| # | EngineId | 类型 | 现实依据 |
|---|----------|------|----------|
| 1 | `liuyao-iching-sixlines` | 内置（已有） | [IChingLibrary.SixLines](https://www.nuget.org/packages/IChingLibrary.SixLines/) 2.0.3，京房纳甲+16 神煞 |
| 2 | `liuyao-npm-bridge` | HTTP 桥接 | [liuyao](https://www.npmjs.com/package/liuyao) 0.3.2，六爻元数据+六神表+变卦 |
| 3 | `liuyao-ichingshifa-bridge` | HTTP 桥接 | [ichingshifa](https://github.com/kentang2017/ichingshifa)，周易筮法/大衍之数/爻辞 |
| 4 | `liuyao-l2yao-bridge` | HTTP 桥接 | [l2yao/iching](https://github.com/l2yao/iching)，八字+六爻对照 |
| 5 | `liuyao-zhouyilab-bridge` | 进程/HTTP 桥接 | [ZhouYiLab](https://github.com/banderzhm/ZhouYiLab) C++23，五术齐全 |
| 6 | `liuyao-yijingframework-annot` | C# 直接引用 | [YiJingFramework.Annotating](https://www.nuget.org/packages/YiJingFramework.Annotating) 5.0.1，《周易》《易传》注解作爻辞数据载体 |

#### Tarot（目标 6）
| # | EngineId | 类型 | 现实依据 |
|---|----------|------|----------|
| 1 | `tarot-builtin` | 内置（已有） | 内置 78 张 + Celtic Cross/Horseshoe |
| 2 | `tarot-deckaura-data` | C# 数据插件 | [tarot-card-meanings](https://www.npmjs.com/package/tarot-card-meanings) Deckaura 12 维牌义数据集（[学术论文 DOI 10.5281/zenodo.19152918](https://cdn.shopify.com/s/files/1/0953/6195/8161/files/Tarot_Interpretation_Systems_Academic_Paper.pdf)），作 Tier 0 牌义库 |
| 3 | `tarot-arcanite-bridge` | HTTP 桥接 | [arcanite](https://pypi.org/project/arcanite/) 0.2.0，11 牌阵+RAG 映射（仅取牌阵/牌义，不用其 LLM） |
| 4 | `tarot-ttarot-bridge` | HTTP 桥接 | [ttarot](https://www.npmjs.com/package/ttarot) 0.3.0，78 牌 upright/reversed 数据 |
| 5 | `tarot-roxyapi-remote` | 远程 API | [RoxyAPI tarot](https://roxyapi.com/docs/tutorials/tarot-app)，商业 tarot API（含 C# SDK） |
| 6 | `tarot-morax-mcp-bridge` | MCP 桥接 | [Tarot MCP Server (Morax)](https://lobehub.com/mcp/morax-tarot-mcp)，11 牌阵+元素平衡 |

#### Calendar（目标 6）
| # | EngineId | 类型 | 现实依据 |
|---|----------|------|----------|
| 1 | `calendar-huangli-builtin` | 内置（已有） | 基于 lunar-csharp 的 [HuangLiService](file:///workspace/src/IChing.Lab.Core/Calendar/HuangLiService.cs) |
| 2 | `calendar-cnlunar-bridge` | HTTP 桥接 | [cnlunar](https://pypi.org/project/cnlunar/)，《钦定协纪辨方书》宜忌等第 |
| 3 | `calendar-lunar-calendar-bridge` | HTTP 桥接 | [lunar-calendar](https://gitcode.com/gh_mirrors/lu/lunar-calendar) VSOP87/LEA-406 天文算法，1901–2100 香港天文台数据 |
| 4 | `calendar-koyomi-remote` | 远程 API | [国立天文台 koyomi](https://eco2.mtk.nao.ac.jp/cgi-bin/koyomi/cande/phenomena_sy.cgi) 二十四节气长期版 |
| 5 | `calendar-lunar-python-bridge` | HTTP 桥接 | [lunar-python](https://github.com/6tail/lunar-python) 节气/杂节 |
| 6 | `calendar-finddays-remote` | 远程 API | [find-days solar-terms](https://find-days.com/solar-terms/) 2026 节气日期表 |

### 3. 配置与发现

- `appsettings.json` 的 `plugins:chartEngines` 声明每域默认引擎 + 备用引擎
- `GET /lab/engines` 返回所有引擎（含 `engineId / domain / metadata.source / metadata.algorithmBasis`）
- 不兼容/桥接服务离线的引擎：`IsReady` 返回 false 且不阻断其他引擎

### 约束

- **不引入 RAG**：桥接插件仅取外部库的**排盘计算结果**（deterministic），不接入其 LLM/RAG 解读层
- 保持「计算 deterministic，解读 generative」边界：所有桥接插件只产 chart JSON，不产解读文本
- 算法语义差异（如 cnlunar 月柱算法）：响应写入 `engine.paipan` 字段供前端展示「按 X 库排盘」

## Impact

- Affected code: [IChing.Lab.Abstractions/Engines/IChartEngine.cs](file:///workspace/src/IChing.Lab.Abstractions/Engines/IChartEngine.cs)（加 metadata）、[IChing.Lab.Core/Engines/](file:///workspace/src/IChing.Lab.Core/Engines/)（4 个内置包装器加 metadata）、新增 `samples/ChartBridge/` 与多个域插件项目、[appsettings.json](file:///workspace/src/IChing.Lab.Api/appsettings.json)
- Affected specs: 依赖 [plugin-abstractions](file:///workspace/.trae/specs/plugin-abstractions/spec.md) + [wrap-chart-engines](file:///workspace/.trae/specs/wrap-chart-engines/spec.md) + [plugin-loader-and-di](file:///workspace/.trae/specs/plugin-loader-and-di/spec.md)（均已完成）
- 被依赖：[algorithm-aware-prompt-templates](file:///workspace/.trae/specs/algorithm-aware-prompt-templates/spec.md) 依赖本 spec 的 `EngineMetadata`

## ADDED Requirements

### Requirement: HTTP 桥接插件模式

系统 SHALL 提供 `ExternalHttpChartBridge` 抽象基类，使外部 Python/Node 算法库可通过 sidecar HTTP 服务包装为 `IChartEngine`。

#### Scenario: 桥接调用
- **WHEN** 调用 `ExternalHttpChartBridge.Calculate(request)`
- **THEN** 引擎 POST `ChartRequest.Args` 到 sidecar 端点，解析 JSON 响应并包装为各域 chart 结果对象
- **AND** sidecar 不可达时 `IsReady=false`，不抛异常

### Requirement: 每域 ≥ 5 排盘引擎

系统 SHALL 为 bazi / liuyao / tarot / calendar 四域各提供 ≥ 5 个 `IChartEngine` 实现，每个实现的 `EngineMetadata` 须标注来源库与算法依据。

#### Scenario: 引擎列举
- **WHEN** GET `/lab/engines`
- **THEN** 每域返回 ≥ 5 条记录，每条含 `engineId / domain / source / algorithmBasis`

#### Scenario: 默认引擎切换
- **WHEN** 修改 `appsettings.json` 的 `plugins:chartEngines[domain=bazi].default` 从 `bazi-lunar-csharp` 改为 `bazi-cnlunar-port`
- **THEN** `/lab/bazi` 使用 cnlunar-port 排盘，响应 `engine.paipan="bazi-cnlunar-port"`

### Requirement: EngineMetadata 审计字段

`IChartEngine` SHALL 暴露 `EngineMetadata`（来源库、版本、算法依据、模块面向），写入排盘响应供前端展示与 Prompt 联动。

#### Scenario: 审计可见
- **WHEN** 任意排盘端点返回响应
- **THEN** 响应含 `engine.paipan` 与 `engine.source`（如 "lunar-csharp 1.6.8"）
