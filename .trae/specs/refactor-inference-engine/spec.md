# Refactor Inference Engine Spec

## Why

`ChartInterpretationService` 直接 `new Model/Tokenizer` 绑死 ONNX GenAI，且同时承担引擎职责与编排职责（塔罗英译中两 pass）。需要拆分为单一职责的 `OnnxGenAiEngine`（实现 `IInferenceEngine`）+ `ChartInterpretationOrchestrator`（编排降级），使后续可插入 LLamaSharp / Ollama / OpenAI 等引擎。

**依赖**：[plugin-abstractions](../plugin-abstractions/spec.md)（需要 `IInferenceEngine` 接口）

## What Changes

- 新建 `OnnxGenAiEngine : IInferenceEngine`，迁移现有 `ChartInterpretationService` 的 ONNX 加载与生成逻辑
- 新建 `TemplateFallbackEngine : IInferenceEngine`，迁移现有 `TemplateFallback` 方法
- 新建 `ChartInterpretationOrchestrator`，承担 `Interpret` / `InterpretTarotEnglishThenChinese` 编排，注入 `IEnumerable<IInferenceEngine>` 选引擎
- **BREAKING**（内部）：`ChartInterpretationService` 标记 `[Obsolete]`，保留 1 个版本以兼容现有测试
- `Program.cs` / DI 注册改为 `services.AddSingleton<IInferenceEngine, OnnxGenAiEngine>()` + `services.AddSingleton<ChartInterpretationOrchestrator>()`
- [LabController](file:///workspace/src/IChing.Lab.Api/Controllers/LabController.cs) 改注入 `ChartInterpretationOrchestrator`
- 现有 `MvpFlowIntegrationTest` / `LabFeatureTests` 必须全绿

## Impact

- Affected specs: plugin-abstractions（依赖）
- Affected code:
  - 新增：`src/IChing.Lab.Inference/Engines/OnnxGenAiEngine.cs` / `TemplateFallbackEngine.cs`
  - 新增：`src/IChing.Lab.Inference/ChartInterpretationOrchestrator.cs`
  - 修改：`src/IChing.Lab.Inference/ChartInterpretationService.cs`（标 Obsolete）
  - 修改：`src/IChing.Lab.Api/Program.cs`（DI 注册）
  - 修改：`src/IChing.Lab.Api/Controllers/LabController.cs`（注入 Orchestrator）

## MODIFIED Requirements

### Requirement: 解读编排与引擎实现分离

The system SHALL separate inference orchestration from engine implementation. `ChartInterpretationOrchestrator` SHALL hold no model loading code; engine implementations SHALL hold no orchestration logic (multi-pass, fallback chain).

#### Scenario: 引擎可独立替换

- **WHEN** 配置从 `onnx-genai` 切换到另一个 `IInferenceEngine` 实现
- **THEN** `ChartInterpretationOrchestrator` 无需修改，塔罗两 pass 流程仍正常执行

### Requirement: 现有 API 行为保持

The system SHALL preserve all existing HTTP response shapes for `/lab/bazi/interpret` / `/lab/liuyao/read` / `/lab/tarot/read`.

#### Scenario: 塔罗英译中两 pass

- **WHEN** 调用 `/lab/tarot/read?tier=1`
- **THEN** 响应 `narrative.text` 仍为中文，`textEn` 仍保留英文初稿，`isFallback` 标记不变
