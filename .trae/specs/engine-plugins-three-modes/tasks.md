# Tasks

## 阶段一：模式 A 进程内引擎

- [ ] Task 1: 创建 LLamaSharpEngine 项目
  - [ ] SubTask 1.1: 新建 `samples/LLamaSharpEngine/LLamaSharpEngine.csproj`（net10.0 类库）
  - [ ] SubTask 1.2: 引用 `IChing.Lab.Abstractions` + `LLamaSharp` + `LLamaSharp.Backend.Cpu`
  - [ ] SubTask 1.3: 编译产出 `LLamaSharpEngine.dll`

- [ ] Task 2: 实现 LLamaSharpEngine
  - [ ] SubTask 2.1: 新建 `LLamaSharpEngine.cs`，实现 `IInferenceEngine`
  - [ ] SubTask 2.2: EngineId="llama-sharp-qwen3-4b"
  - [ ] SubTask 2.3: 构造注入 modelPath / gpuLayerCount（从 `IConfiguration` 解析）
  - [ ] SubTask 2.4: `IsReady` 返回 model 加载完成
  - [ ] SubTask 2.5: `GenerateAsync` 调用 `LLamaWeights.CreateChat` + `ChatSession`

## 阶段二：模式 B + C 共享基类与本地 HTTP

- [ ] Task 3: 创建 OpenAiCompatibleEngine 项目
  - [ ] SubTask 3.1: 新建 `samples/OpenAiCompatibleEngine/OpenAiCompatibleEngine.csproj`
  - [ ] SubTask 3.2: 引用 `IChing.Lab.Abstractions` + `Microsoft.Extensions.Http`
  - [ ] SubTask 3.3: 编译产出 `OpenAiCompatibleEngine.dll`

- [ ] Task 4: 实现 OpenAiCompatibleEngineBase
  - [ ] SubTask 4.1: 新建 `OpenAiCompatibleEngineBase.cs`（abstract，实现 `IInferenceEngine`）
  - [ ] SubTask 4.2: 抽象属性 `Client`（HttpClient）/ `ModelName` / `ApiKey`
  - [ ] SubTask 4.3: `GenerateAsync` POST `/v1/chat/completions`，解析 `choices[0].message.content`
  - [ ] SubTask 4.4: 解析 ChatML prompt 为 `messages` 数组（system / user 分离）
  - [ ] SubTask 4.5: 超时与错误处理（HTTP 5xx → `IsReady=false`）

- [ ] Task 5: 实现 OllamaLocalEngine 与 LlamaServerLocalEngine
  - [ ] SubTask 5.1: `OllamaLocalEngine`：EngineId="ollama-local"，BaseUrl 从配置读
  - [ ] SubTask 5.2: `LlamaServerLocalEngine`：EngineId="llama-server-local"
  - [ ] SubTask 5.3: `IsReady` 通过 `GET /api/tags`（Ollama）或 `/v1/models`（llama-server）探活

## 阶段三：模式 C 远程 API

- [ ] Task 6: 实现 OpenAiRemoteEngine 与 AzureOpenAiEngine
  - [ ] SubTask 6.1: `OpenAiRemoteEngine`：EngineId="openai-remote"，整合 [OpenAiChatClient](file:///workspace/src/IChing.Desktop/OpenAiChatClient.cs) 逻辑
  - [ ] SubTask 6.2: `AzureOpenAiEngine`：EngineId="azure-openai-remote"，URL 格式 `{baseUrl}/openai/deployments/{model}/chat/completions?api-version=...`
  - [ ] SubTask 6.3: API key 从 `IConfiguration[apiKeyKey]` 读，不入仓
  - [ ] SubTask 6.4: 文档说明 User Secrets 配置方式（`dotnet user-secrets set "OpenAI:ApiKey" "..."`）

## 阶段四：降级链编排

- [ ] Task 7: 修改 ChartInterpretationOrchestrator
  - [ ] SubTask 7.1: 注入 `IConfiguration`，读 `plugins:fallbackChain` 数组
  - [ ] SubTask 7.2: 按 chain 顺序遍历引擎，调 `IsReady` + `GenerateAsync`
  - [ ] SubTask 7.3: 任一成功即返回，记录 `engine.used`
  - [ ] SubTask 7.4: 全失败时用 `TemplateFallbackEngine`，`isFallback=true`，`fallbackReason` 描述

- [ ] Task 8: 新增健康检查端点
  - [ ] SubTask 8.1: `LabController` 新增 `GET /health/engines`
  - [ ] SubTask 8.2: 返回 JSON 数组 `[{engineId, mode, isReady, isDefault}]`
  - [ ] SubTask 8.3: 不需要鉴权（内部探活用）

## 阶段五：配置与验证

- [ ] Task 9: 完善 appsettings.json
  - [ ] SubTask 9.1: 加完整 `plugins:inferenceEngines` 数组（6 个引擎示例）
  - [ ] SubTask 9.2: 加 `plugins:fallbackChain: ["onnx-genai-qwen2.5-1.5b", "ollama-local", "openai-remote", "template-fallback"]`
  - [ ] SubTask 9.3: 加 `plugins:externalAssemblies` 注册 LLamaSharpEngine.dll / OpenAiCompatibleEngine.dll

- [ ] Task 10: 端到端验证
  - [ ] SubTask 10.1: `dotnet build` 全绿（主程序 + 2 个 samples 项目）
  - [ ] SubSub 10.2: 拷贝 samples DLL 到 plugins/，启动主程序，`/health/engines` 返回 6 个引擎
  - [ ] SubTask 10.3: 启动 Ollama 本地服务，`/lab/bazi/read?tier=1` 配置 ollama-local 为 default，验证返回解读
  - [ ] SubTask 10.4: 关闭 Ollama，验证降级到 template-fallback，`isFallback=true`
  - [ ] SubTask 10.5: 配置 OpenAI API key（User Secrets），切换 default=openai-remote，验证远程调用
  - [ ] SubTask 10.6: 检查 git 仓库无明文 API key（`git grep -i "sk-"` 应无结果）
  - [ ] SubTask 10.7: `dotnet test` 全绿

# Task Dependencies
- 依赖 [plugin-abstractions](../plugin-abstractions/spec.md) / [refactor-inference-engine](../refactor-inference-engine/spec.md) / [plugin-loader-and-di](../plugin-loader-and-di/spec.md) 完成
- Task 1 → Task 2（模式 A）
- Task 3 → Task 4 → Task 5（模式 B）/ Task 6（模式 C，可与 Task 5 并行）
- Task 7 依赖 Task 2 / 5 / 6 完成
- Task 8 依赖 Task 7
- Task 9 与 Task 7/8 并行
- Task 10 依赖全部完成
