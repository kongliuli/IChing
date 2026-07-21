# Wrap Chart Engines Spec

## Why

[BaziEngine](../../../../src/IChing.Lab.Core/Bazi/BaziEngine.cs) / [LiuyaoNajiaService](../../../../src/IChing.Lab.Core/Liuyao/LiuyaoNajiaService.cs) / [TarotEngine](../../../../src/IChing.Lab.Core/Tarot/TarotEngine.cs) 均为 `static` 类，无法注册到 DI 容器，也无法被同名 domain 下多个实现替换。需要包装为 `IChartEngine` 实现并保留向下兼容（原 static API 不变）。

**依赖**：[plugin-abstractions](../plugin-abstractions/spec.md)（需要 `IChartEngine` 接口）

## What Changes

- 新建三个包装类，均实现 `IChartEngine`：
  - `BaziChartEngine`（Domain="bazi"，EngineId="lunar-csharp-1.6.8"）
  - `LiuyaoChartEngine`（Domain="liuyao"，EngineId="iching-sixlines-2.0.3"）
  - `TarotChartEngine`（Domain="tarot"，EngineId="iching-tarot-built-in"）
  - `CalendarEngine`（Domain="calendar"，EngineId="lunar-csharp-1.6.8"）
- 包装类内部委托给原 static 方法（不改算法）
- 注册到 DI 容器：`services.AddSingleton<IChartEngine, BaziChartEngine>()` 等
- [LabController](../../../../src/IChing.Lab.Api/Controllers/LabController.cs) 保留原 static 调用（向下兼容）；同时新增一个示范端点 `/lab/engines` 列出已注册引擎
- **不破坏**：原 static API 完全保留，现有测试不动

## Impact

- Affected specs: plugin-abstractions（依赖）
- Affected code:
  - 新增：`src/IChing.Lab.Core/Engines/BaziChartEngine.cs` / `LiuyaoChartEngine.cs` / `TarotChartEngine.cs` / `CalendarEngine.cs`
  - 修改：`src/IChing.Lab.Api/Program.cs`（注册包装类）
  - 修改：`src/IChing.Lab.Api/Controllers/LabController.cs`（新增 `/lab/engines` 端点）

## ADDED Requirements

### Requirement: 排盘引擎可注册到 DI

The system SHALL wrap existing static chart engines into `IChartEngine` implementations registered in DI container.

#### Scenario: 同 domain 多实现并存

- **WHEN** DI 中注册两个 `IChartEngine` 且 Domain="bazi"
- **THEN** `/lab/engines` 返回两条记录，按 EngineId 区分

### Requirement: 向下兼容

The system SHALL preserve all existing static method signatures on `BaziEngine` / `LiuyaoNajiaService` / `TarotEngine` / `HuangLiService`.

#### Scenario: 现有调用不变

- **WHEN** `LabController.Bazi` 调用 `BaziEngine.Calculate(input)`
- **THEN** 编译通过且行为不变
