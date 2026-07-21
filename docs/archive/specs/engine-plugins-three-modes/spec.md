# Engine Plugins Three Modes Spec

## Why

[plugin-design.md §4.4.3](../../../active/plugin-design.md) 定义了三类 AI 调用模式（进程内 / 本地 HTTP / 远程 API），但当前仅模式 A 的 ONNX GenAI 落地。需要实现剩余插件，使所有引擎统一为 `IInferenceEngine` 实现，并构建降级链。

**依赖**：
- [plugin-abstractions](../plugin-abstractions/spec.md)（`IInferenceEngine`）
- [refactor-inference-engine](../refactor-inference-engine/spec.md)（Orchestrator 已存在）
- [plugin-loader-and-di](../plugin-loader-and-di/spec.md)（外部插件加载机制）

## What Changes

### 模式 A 进程内（外部插件）
- 新建 `samples/LLamaSharpEngine/` 项目（独立可编译，引用 `LLamaSharp` + `LLamaSharp.Backend.Cpu`）
- 实现 `LLamaSharpEngine : IInferenceEngine`，EngineId="llama-sharp-qwen3-4b"
- 模型路径从 `appsettings.json` 读取（`plugins:inferenceEngines[id=llama-sharp-qwen3-4b].modelPath`）

### 模式 B 本地 HTTP（外部插件，共享基类）
- 新建 `samples/OpenAiCompatibleEngine/` 项目，含：
  - `OpenAiCompatibleEngineBase`（abstract，HTTP + OpenAI 兼容协议）
  - `OllamaLocalEngine`（EngineId="ollama-local"，BaseUrl=localhost:11434）
  - `LlamaServerLocalEngine`（EngineId="llama-server-local"，BaseUrl=localhost:8080）

### 模式 C 远程 API（外部插件，复用基类）
- 在 `samples/OpenAiCompatibleEngine/` 内加：
  - `OpenAiRemoteEngine`（EngineId="openai-remote"，整合 [OpenAiChatClient](../../../../src/IChing.Desktop/OpenAiChatClient.cs) 逻辑）
  - `AzureOpenAiEngine`（EngineId="azure-openai-remote"，Azure 端点格式）
- API key 走 `IConfiguration` + User Secrets，不入仓

### 降级链编排
- 修改 `ChartInterpretationOrchestrator`，注入 `IConfiguration` 读 `plugins:fallbackChain`
- 按 chain 顺序尝试 `IsReady` + `GenerateAsync`，任一成功即返回
- 全失败时使用 `TemplateFallbackEngine`，`isFallback=true`
- 新增 `GET /health/engines` 端点：返回每个引擎 `IsReady` 状态

### 配置 schema
- `appsettings.json` 加完整 `plugins:inferenceEngines` 数组（见 [plugin-design.md §5.3](../../../active/plugin-design.md)）
- 每个 engine 含 `mode` 字段（InProcess / LocalHttp / RemoteApi）

## Impact

- Affected specs:
  - plugin-abstractions / refactor-inference-engine / plugin-loader-and-di（依赖）
- Affected code:
  - 新增：`samples/LLamaSharpEngine/`（独立项目）
  - 新增：`samples/OpenAiCompatibleEngine/`（独立项目，含 4 个引擎类 + 基类）
  - 修改：`src/IChing.Lab.Inference/ChartInterpretationOrchestrator.cs`（降级链编排）
  - 修改：`src/IChing.Lab.Api/Controllers/LabController.cs`（新增 `/health/engines`）
  - 修改：`src/IChing.Lab.Api/appsettings.json`（完整 plugins 段）

## ADDED Requirements

### Requirement: 模式 A 进程内引擎插件

The system SHALL provide `LLamaSharpEngine` as an external plugin loaded via `PluginLoader`, supporting GGUF models with CPU/CUDA backend.

#### Scenario: 切换到 LLamaSharp

- **WHEN** `appsettings.json` 设置 `plugins:inferenceEngines[id=llama-sharp-qwen3-4b].default=true`
- **THEN** `/lab/bazi/read?tier=1` 使用 LLamaSharp 引擎生成解读，响应 `engine.used="llama-sharp-qwen3-4b"`

### Requirement: 模式 B 本地 HTTP 引擎插件

The system SHALL provide `OllamaLocalEngine` and `LlamaServerLocalEngine` plugins reusing `OpenAiCompatibleEngineBase`.

#### Scenario: Ollama 本地调用

- **WHEN** 用户本地运行 `ollama serve` + `ollama pull qwen2.5:7b`，配置 `plugins:inferenceEngines[id=ollama-local]` 启用
- **THEN** 请求经 HTTP 调用 `http://localhost:11434/v1/chat/completions`，返回解读

### Requirement: 模式 C 远程 API 引擎插件

The system SHALL provide `OpenAiRemoteEngine` and `AzureOpenAiEngine` plugins.

#### Scenario: OpenAI 远程调用

- **WHEN** 配置 `plugins:inferenceEngines[id=openai-remote]` 含 `apiKeyKey="OpenAI:ApiKey"`
- **THEN** API key 从 `IConfiguration["OpenAI:ApiKey"]` 读取，请求发往 `https://api.openai.com/v1`

#### Scenario: API key 不入仓

- **WHEN** 检查 git 仓库
- **THEN** 任何 `appsettings.json` 或代码中均不含明文 API key

### Requirement: 降级链编排

The `ChartInterpretationOrchestrator` SHALL try engines in `plugins:fallbackChain` order, falling back to `TemplateFallbackEngine` when all fail.

#### Scenario: ONNX 不可用降级到 Ollama

- **WHEN** `onnx-genai-qwen2.5-1.5b.IsReady=false`，`ollama-local.IsReady=true`
- **THEN** 请求自动使用 ollama-local，响应 `engine.used="ollama-local"`，`isFallback=false`

#### Scenario: 全部引擎不可用

- **WHEN** fallbackChain 中所有引擎 `IsReady=false`
- **THEN** 使用 `template-fallback`，响应 `isFallback=true`，`fallbackReason` 描述失败原因

### Requirement: 健康检查端点

The system SHALL expose `GET /health/engines` returning each engine's `EngineId` / `IsReady` / `Mode`.

#### Scenario: 列出所有引擎状态

- **WHEN** 调用 `/health/engines`
- **THEN** 返回 JSON 数组，每项含 `engineId` / `mode` / `isReady` / `default`

## MODIFIED Requirements

### Requirement: Orchestrator 按配置链降级

The `ChartInterpretationOrchestrator` SHALL no longer hardcode engine selection; it SHALL read `plugins:fallbackChain` from configuration.

#### Scenario: 修改降级链不需重编译

- **WHEN** 编辑 `appsettings.json` 中 `plugins:fallbackChain` 数组顺序
- **THEN** 重启后下次请求按新顺序尝试引擎
