# DeepSeek 远程解读引擎插件 Spec

## Why

当前降级链 `onnx-genai → ollama-local → openai-remote → template-fallback` 中，远程 API 引擎只有 `openai-remote`（需 OpenAI key，沙箱无）。为方便在无本地模型、无 OpenAI key 的环境下端到端验证降级链与 Tier 2 解读，需要接入一个**国内可直连、OpenAI 兼容、成本低**的远程引擎。DeepSeek API（`https://api.deepseek.com/v1`）符合该定位；历史测试 key 已从文档中移除。

## What Changes

- 在 `samples/OpenAiCompatibleEngine` 新增 `DeepSeekEngine`，继承 `OpenAiCompatibleEngineBase`
- `EngineId = "deepseek-remote"`，`mode = "remote-api"`
- `BaseUrl = "https://api.deepseek.com/v1"`，`ModelName = "deepseek-chat"`
- **API key 硬编码** `<redacted-test-key>` 于引擎内部（历史测试方案；现已改为配置注入），代码注释标注 `TEST-ONLY / 生产环境须改为 IConfiguration + User Secrets`
- 在 `EngineModule.Register` 注册 `DeepSeekEngine` 到 DI（集合形式）
- `appsettings.json` 的 `plugins:inferenceEngines` 数组新增 `deepseek-remote` 条目
- `plugins:fallbackChain` 追加 `deepseek-remote`（置于 `openai-remote` 之前，便于国内环境优先命中）
- `/health/engines` 自动包含新引擎（已有逻辑按 `IEnumerable<IInferenceEngine>` 聚合，无需改控制器）
- 新增单测：mock `HttpClient` 验证 DeepSeek 请求体（`model=deepseek-chat`）与响应解析（`choices[0].message.content`）

### 约束

- **不引入 RAG**：DeepSeek 引擎仅作纯生成解读，不做知识库检索（遵循用户「解读引擎暂不考虑 RAG」约束）
- 不改动 `OpenAiCompatibleEngineBase` 既有契约，仅新增子类

## Impact

- Affected code: [samples/OpenAiCompatibleEngine/](../../../../samples/OpenAiCompatibleEngine/)（新增 `DeepSeekEngine.cs` + 修改 `EngineModule.cs`）、[appsettings.json](../../../../src/IChing.Lab.Api/appsettings.json)、[IChing.Lab.Tests](../../../../src/IChing.Lab.Tests/)
- Affected specs: 依赖 [engine-plugins-three-modes](../engine-plugins-three-modes/spec.md)（已完成，提供基类与降级链）
- 无破坏性变更：新引擎仅追加，不替换既有默认引擎 `onnx-genai-qwen2.5-1.5b`

## ADDED Requirements

### Requirement: DeepSeek 远程解读引擎

系统 SHALL 提供 `deepseek-remote` 解读引擎，作为 `IInferenceEngine` 的实现，复用 `OpenAiCompatibleEngineBase` 的 ChatML 解析与 `/v1/chat/completions` 调用逻辑。

#### Scenario: 调用成功
- **WHEN** 调用方以 `EngineId="deepseek-remote"` 发起 `GenerateAsync`
- **THEN** 引擎向 `https://api.deepseek.com/v1/chat/completions` POST，请求体含 `model="deepseek-chat"` 与 `Authorization: Bearer <redacted-test-key>`
- **AND** 解析 `choices[0].message.content` 返回 `GenerationResult(IsFallback=false)`

#### Scenario: 降级链命中
- **WHEN** `onnx-genai` 与 `ollama-local` 均 `IsReady=false`
- **AND** `fallbackChain` 含 `deepseek-remote`
- **THEN** `ChartInterpretationOrchestrator` 调用 `deepseek-remote`，成功则返回其结果并记录 `engine.used="deepseek-remote"`

#### Scenario: 健康检查可见
- **WHEN** GET `/health/engines`
- **THEN** 响应数组含 `{engineId:"deepseek-remote", mode:"remote-api", isReady:..., isDefault:false}`

### Requirement: 测试用 API key 硬编码

历史方案要求引擎在内部以常量持有测试 key；当前实现已改为配置注入，文档中的旧值已清除。

#### Scenario: key 注入
- **WHEN** `DeepSeekEngine` 构造 `HttpClient`
- **THEN** 默认 `Authorization` 头使用硬编码 key
- **AND** 代码注释明确标注 `TEST-ONLY`，并在 `appsettings.json` 留 `apiKeyKey` 占位以便生产环境切换到 User Secrets

## MODIFIED Requirements

### Requirement: 降级链配置

`plugins:fallbackChain` SHALL 包含 `deepseek-remote`，位置在 `openai-remote` 之前（国内环境优先命中 DeepSeek，避免 OpenAI 公网不可达导致整链失败）。

新链：`onnx-genai-qwen2.5-1.5b → ollama-local → deepseek-remote → openai-remote → template-fallback`
