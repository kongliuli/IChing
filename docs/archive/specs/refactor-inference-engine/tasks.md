# Tasks

- [x] Task 1: 创建 OnnxGenAiEngine
  - [x] SubTask 1.1: 新建 `Engines/OnnxGenAiEngine.cs`，实现 `IInferenceEngine`
  - [x] SubTask 1.2: 迁移 `ChartInterpretationService` 的 `EnsureModel` / `RunGeneration` / `Generate` 逻辑
  - [x] SubTask 1.3: `EngineId = "onnx-genai-qwen2.5-1.5b"`，`IsReady` 返回 `_model is not null`

- [x] Task 2: 创建 TemplateFallbackEngine
  - [x] SubTask 2.1: 新建 `Engines/TemplateFallbackEngine.cs`，实现 `IInferenceEngine`
  - [x] SubTask 2.2: 迁移 `TemplateFallback` 方法逻辑
  - [x] SubTask 2.3: `EngineId = "template-fallback"`，`IsReady` 永远 true

- [x] Task 3: 创建 ChartInterpretationOrchestrator
  - [x] SubTask 3.1: 新建 `ChartInterpretationOrchestrator.cs`
  - [x] SubTask 3.2: 注入 `IEnumerable<IInferenceEngine>` + `ILogger`
  - [x] SubTask 3.3: 迁移 `Interpret` 方法（按 EngineId 选择引擎）
  - [x] SubTask 3.4: 迁移 `InterpretTarotEnglishThenChinese`（塔罗两 pass，复用同一引擎）
  - [x] SubTask 3.5: 迁移 `RunFixture`（PromptTest 用）

- [x] Task 4: 标记旧服务 Obsolete 并切换 DI
  - [x] SubTask 4.1: `ChartInterpretationService` 加 `[Obsolete("Use ChartInterpretationOrchestrator + IInferenceEngine")]`
  - [x] SubTask 4.2: `Program.cs` 注册 `OnnxGenAiEngine` / `TemplateFallbackEngine` / `ChartInterpretationOrchestrator`
  - [x] SubTask 4.3: `LabController` 改注 `ChartInterpretationOrchestrator`

- [x] Task 5: 验证现有测试全绿
  - [x] SubTask 5.1: `dotnet test` 全绿（含 `MvpFlowIntegrationTest` / [LabFeatureTests](../../../../src/IChing.Lab.Tests/LabFeatureTests.cs)）
  - [x] SubTask 5.2: 跑 dry-run fixture（`dotnet run --project IChing.Lab.PromptTest -- --dry-run`）输出完整 Prompt
  - [x] SubTask 5.3: API 响应 JSON 结构与改造前一致（人工 diff）

# Task Dependencies
- 依赖 [plugin-abstractions](../plugin-abstractions/spec.md) 完成
- Task 1 / Task 2 可并行
- Task 3 依赖 Task 1 / Task 2
- Task 4 依赖 Task 3
- Task 5 依赖 Task 4
