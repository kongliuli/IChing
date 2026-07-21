# Plugin Abstractions Spec

## Why

当前 `IChing.Lab.*` 三大耦合点（排盘算法 / Prompt Builder / 解读引擎）均为硬编码静态绑定，无法替换实现。需要先定义共享抽象层，作为后续插件化的契约基础，避免各插件与主程序循环依赖。

## What Changes

- 新建 `IChing.Lab.Abstractions` 项目（.NET 10 类库，独立 DLL）
- 定义四接口：
  - `IChartEngine` — 排盘算法抽象
  - `IPromptBuilder` — Prompt 模板抽象
  - `IInferenceEngine` — 解读引擎抽象
  - `IPluginModule` — 插件模块自注册入口
- 定义共享 record/DTO：`ChartRequest` / `PromptContext` / `PromptBuildResult` / `GenerateOptions` / `GenerationResult`
- 定义 `IPluginManifest`（含 `RequiredApiVersion`）用于启动校验
- 加入 `src/IChing.Lab.sln`
- 不修改任何现有代码，只增抽象

## Impact

- Affected specs: 为后续 5 个 spec 提供契约基础
- Affected code: 新增 `src/IChing.Lab.Abstractions/` 目录；`IChing.Lab.sln` 增加项目引用

## ADDED Requirements

### Requirement: 抽象项目独立可编译

The system SHALL provide `IChing.Lab.Abstractions` as a standalone .NET 10 class library that compiles independently and has zero dependencies on other `IChing.Lab.*` projects.

#### Scenario: 仅引用抽象项目即可定义插件

- **WHEN** 开发者创建一个外部插件项目，仅引用 `IChing.Lab.Abstractions.dll`
- **THEN** 项目可编译通过，且能实现 `IInferenceEngine` / `IChartEngine` / `IPromptBuilder` / `IPluginModule` 任一接口

### Requirement: 接口覆盖三对象

The system SHALL define interfaces covering chart engine, prompt builder, inference engine, and plugin module.

#### Scenario: IChartEngine

- **WHEN** 实现 `IChartEngine`
- **THEN** 必须提供 `Domain` / `EngineId` 属性和 `Calculate(ChartRequest)` 方法

#### Scenario: IInferenceEngine

- **WHEN** 实现 `IInferenceEngine`
- **THEN** 必须实现 `IDisposable`，提供 `EngineId` / `IsReady` 属性和 `GenerateAsync` 方法

### Requirement: 启动版本校验

The system SHALL provide `IPluginManifest` with `RequiredApiVersion` for runtime compatibility check.

#### Scenario: 接口版本不匹配

- **WHEN** 插件 `RequiredApiVersion` 与主程序 `AbstractionsVersion` 不兼容
- **THEN** 主程序启动时拒绝加载该插件并记录警告日志
