# Externalize Prompt Templates Spec

## Why

[BaziPromptBuilder](file:///workspace/src/IChing.Lab.Inference/Prompts/BaziPromptBuilder.cs) / [LiuyaoPromptBuilder](file:///workspace/src/IChing.Lab.Inference/Prompts/LiuyaoPromptBuilder.cs) / [TarotPromptBuilder](file:///workspace/src/IChing.Lab.Inference/Prompts/TarotPromptBuilder.cs) 均为 `static` 类，模板字符串硬编码，改 prompt 必须重编译。需要外置到 `prompts/*.txt` 文件，使用 Scriban 模板引擎渲染，支持运行时热加载。

**依赖**：[plugin-abstractions](../plugin-abstractions/spec.md)（需要 `IPromptBuilder` 接口）

## What Changes

- 引入 [Scriban](https://github.com/scriban/scriban) NuGet 包
- 新建 `prompts/` 目录（仓库根），存放模板文件：
  - `bazi-tier1-default.txt`
  - `liuyao-tier1-default.txt`
  - `tarot-tier1-en.txt`
  - `tarot-translate-to-zh.txt`
- 新建 `TemplatePromptBuilder : IPromptBuilder` 实现，按 `(domain, tier, templateId)` 加载模板
- 新建 `PromptTemplateRegistry`：启动扫描 + `FileSystemWatcher` 热重载
- 迁移 3 个 PromptBuilder 的字符串到模板文件，原 `static` 方法标 `[Obsolete]`
- `ChartInterpretationOrchestrator` 改注入 `IPromptBuilder`（来自 refactor-inference-engine）
- Tier 0 模板不走此机制（仍在 [ReadingSummaries](file:///workspace/src/IChing.Lab.Core/Readings/ReadingSummaries.cs)）

## Impact

- Affected specs:
  - plugin-abstractions（依赖）
  - refactor-inference-engine（Orchestrator 改注入）
- Affected code:
  - 新增：`prompts/*.txt`（4 个模板文件）
  - 新增：`src/IChing.Lab.Inference/Prompts/TemplatePromptBuilder.cs`
  - 新增：`src/IChing.Lab.Inference/Prompts/PromptTemplateRegistry.cs`
  - 修改：`src/IChing.Lab.Inference/Prompts/BaziPromptBuilder.cs` 等 3 个（标 Obsolete，保留向下兼容）
  - 修改：`src/IChing.Lab.Inference/IChing.Lab.Inference.csproj`（加 Scriban 依赖）
  - 修改：`src/IChing.Lab.Api/Program.cs`（注册 `IPromptBuilder`）

## ADDED Requirements

### Requirement: 模板文件外置

The system SHALL store prompt templates as `.txt` files under `prompts/` directory, loaded at runtime via Scriban.

#### Scenario: 修改模板不需重编译

- **WHEN** 编辑 `prompts/bazi-tier1-default.txt` 中的措辞
- **THEN** 下次 `/lab/bazi/read?tier=1` 请求使用新模板，无需 `dotnet build`

### Requirement: 热重载

The system SHALL watch `prompts/` directory for changes and reload affected templates without restart.

#### Scenario: 文件变更触发重载

- **WHEN** 运行时 `prompts/tarot-tier1-en.txt` 被修改保存
- **THEN** 5 秒内 `PromptTemplateRegistry` 重载该模板，日志记录 reload 事件

### Requirement: 模板加载失败降级

The system SHALL fall back to embedded default template when external file is missing or malformed.

#### Scenario: 模板文件被删除

- **WHEN** `prompts/bazi-tier1-default.txt` 不存在
- **THEN** 使用内嵌默认模板，日志记录 warning，请求不失败

## MODIFIED Requirements

### Requirement: Prompt 构建由 IPromptBuilder 承担

The `ChartInterpretationOrchestrator` SHALL consume `IPromptBuilder` instead of calling static `BaziPromptBuilder.BuildTier1` directly.

#### Scenario: Tier 1 八字 prompt

- **WHEN** 调用 `/lab/bazi/read?tier=1`
- **THEN** Orchestrator 通过 `IPromptBuilder.Build(PromptContext)` 取得 prompt 文本
