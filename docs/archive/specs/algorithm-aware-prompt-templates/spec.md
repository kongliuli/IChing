# 算法感知的 Prompt 模板 Spec

## Why

当前 Prompt 模板仅按 `domain + tier` 选择（如 `bazi-tier1-default.txt`），与具体排盘引擎无关。但不同算法插件产生的结果结构与侧重不同（如 cnlunar 强调宜忌等第、openfate 强调真太阳时与大运交互、IChingLibrary 强调纳甲神煞），应驱动选择更贴合的模板。用户要求：「A 算法插件 → 规则引擎产生的 a 结果 → 构造甲 prompt；多个面向模块的算法组合产生更贴合的 prompt」。

## What Changes

### 1. EngineMetadata 流入 PromptContext

- `IChartEngine` 输出 `EngineMetadata`（含 `EngineId / Source / ModuleFocus`，见 [expand-chart-algorithm-plugins](../expand-chart-algorithm-plugins/spec.md)）
- 排盘响应附带 `engine.metadata` 字段
- `PromptContext` 新增 `EngineMetadata? Engine` 与 `IReadOnlyList<string> ModuleFocuses` 字段
- `ChartInterpretationOrchestrator` 把排盘引擎的 metadata 传入 `PromptContext`

### 2. 模板命名与选择规则扩展

- `PromptTemplateRegistry` 支持三级回退命名：`{domain}-tier{N}-{engineVariant}-{module}.txt` → `{domain}-tier{N}-{engineVariant}.txt` → `{domain}-tier{N}-default.txt`
- `engineVariant` 取自 `EngineMetadata.TemplateHint`（如 `cnlunar` / `openfate` / `sixlines`）
- `module` 取自 `ModuleFocuses`（如 `yongshen` / `geju` / `najia`）；多模块时按优先级取首个有对应模板的

### 3. 多模块组合 Prompt

- 当一次解读启用多个面向模块的算法（如 bazi 同时跑 `geju` + `yongshen` 两个模块引擎），Orchestrator 调用对应模块模板生成片段，再由「组合模板」`{domain}-tier{N}-combined.txt` 拼装
- 组合模板用 Scriban `{{ module_snippets.yongshen }}` / `{{ module_snippets.geju }}` 占位

### 4. 新增面向模块的模板文件

| 模板 | 用途 |
|------|------|
| `bazi-tier1-cnlunar-default.txt` | cnlunar 引擎：强调宜忌等第 |
| `bazi-tier1-openfate-default.txt` | openfate 引擎：强调真太阳时/大运交互 |
| `bazi-tier1-lunar-default.txt` | lunar-csharp 引擎：默认四柱+十神 |
| `bazi-tier1-lunar-yongshen.txt` | lunar 引擎 + 用神模块 |
| `bazi-tier1-lunar-geju.txt` | lunar 引擎 + 格局模块 |
| `bazi-tier1-combined.txt` | 多模块组合拼装 |
| `liuyao-tier1-sixlines-najia.txt` | 六爻纳甲模块 |
| `liuyao-tier1-sixlines-shensha.txt` | 六爻神煞模块 |
| `tarot-tier1-deckaura-default.txt` | Deckaura 12 维牌义 |

### 约束

- **不引入 RAG**：模板选择基于算法 metadata 与模块面向，不做向量检索
- 向下兼容：无 `engineVariant` 模板时回退到 `{domain}-tier{N}-default.txt`，行为与改造前一致
- 原 3 个 static PromptBuilder 已标 `[Obsolete]`，本 spec 不复活它们

## Impact

- Affected code: [IChing.Lab.Abstractions/Models/PromptContext.cs](../../../../src/IChing.Lab.Abstractions/Models/PromptContext.cs)、[IChing.Lab.Abstractions/Plugins/IPluginManifest.cs](../../../../src/IChing.Lab.Abstractions/Plugins/IPluginManifest.cs) 邻近的 metadata、[IChing.Lab.Inference/Prompts/PromptTemplateRegistry.cs](../../../../src/IChing.Lab.Inference/Prompts/PromptTemplateRegistry.cs)、[ChartInterpretationOrchestrator.cs](../../../../src/IChing.Lab.Inference/ChartInterpretationOrchestrator.cs)、[prompts/](../../../../prompts/)
- Affected specs: 依赖 [expand-chart-algorithm-plugins](../expand-chart-algorithm-plugins/spec.md)（提供 `EngineMetadata`）+ [externalize-prompt-templates](../externalize-prompt-templates/spec.md)（已完成，提供 Scriban + 热重载）

## ADDED Requirements

### Requirement: 算法感知模板选择

`IPromptBuilder` SHALL 根据 `PromptContext.Engine.TemplateHint` 与 `PromptContext.ModuleFocuses` 选择模板，按三级回退命名解析。

#### Scenario: 引擎特定模板命中
- **WHEN** 排盘使用 `bazi-cnlunar-port`（TemplateHint="cnlunar"）
- **AND** `prompts/bazi-tier1-cnlunar-default.txt` 存在
- **THEN** `IPromptBuilder` 选用该模板
- **AND** 模板内可引用 cnlunar 特有的宜忌等第字段

#### Scenario: 三级回退
- **WHEN** 排盘使用 `bazi-openfate-bridge`（TemplateHint="openfate"）
- **AND** `bazi-tier1-openfate-yongshen.txt` 不存在，`bazi-tier1-openfate-default.txt` 存在
- **THEN** 回退到 `bazi-tier1-openfate-default.txt`

#### Scenario: 完全回退兼容
- **WHEN** 排盘引擎未提供 `TemplateHint`
- **THEN** 回退到 `bazi-tier1-default.txt`，行为与改造前一致

### Requirement: 多模块组合 Prompt

当一次解读启用多个面向模块的算法时，Orchestrator SHALL 调用各模块模板生成片段，再用组合模板拼装为最终 prompt。

#### Scenario: 双模块组合
- **WHEN** bazi 解读同时启用 `geju` 与 `yongshen` 模块
- **THEN** Orchestrator 分别用 `bazi-tier1-lunar-geju.txt` 与 `bazi-tier1-lunar-yongshen.txt` 生成片段
- **AND** 用 `bazi-tier1-combined.txt` 拼装，`{{ module_snippets.geju }}` / `{{ module_snippets.yongshen }}` 被片段替换
- **AND** 最终 prompt 同时含格局与用神分析，更贴合多模块算法产出
