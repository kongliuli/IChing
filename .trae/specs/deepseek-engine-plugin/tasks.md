# Tasks

- [x] Task 1: 实现 DeepSeekEngine 引擎类
  - [x] SubTask 1.1: 在 `samples/OpenAiCompatibleEngine/` 新增 `DeepSeekEngine.cs`，继承 `OpenAiCompatibleEngineBase`
  - [x] SubTask 1.2: `EngineId="deepseek-remote"`，`ModelName="deepseek-chat"`，`ApiEndpoint="/v1/chat/completions"`
  - [x] SubTask 1.3: 构造 `HttpClient` 时设 `BaseAddress="https://api.deepseek.com/v1"` + 默认 `Authorization: Bearer sk-2c248bc685c144739c88181fb665d89d`，注释标注 `TEST-ONLY`
  - [x] SubTask 1.4: 实现 `ProbeIsReady` 返回 true（远程 API 不主动探活）
  - [x] SubTask 1.5: `appsettings.json` 留 `apiKeyKey="DeepSeek:ApiKey"` 占位（生产切换 User Secrets）

- [x] Task 2: 注册 DeepSeekEngine 到 DI
  - [x] SubTask 2.1: `EngineModule.Register` 追加 `services.AddSingleton<IInferenceEngine, DeepSeekEngine>()`
  - [x] SubTask 2.2: `appsettings.json` 的 `plugins:inferenceEngines` 追加 `deepseek-remote` 条目（mode=remote-api, baseUrl, model=deepseek-chat）

- [x] Task 3: 调整降级链
  - [x] SubTask 3.1: `plugins:fallbackChain` 改为 `["onnx-genai-qwen2.5-1.5b", "ollama-local", "deepseek-remote", "openai-remote", "template-fallback"]`

- [x] Task 4: 单元测试
  - [x] SubTask 4.1: 新增 `DeepSeekEngineTests`，mock `HttpMessageHandler` 验证请求体含 `model="deepseek-chat"` 与 `Authorization` 头
  - [x] SubTask 4.2: 验证响应解析 `choices[0].message.content` 返回 `IsFallback=false`
  - [x] SubTask 4.3: 验证 HTTP 5xx 时返回 `IsFallback=true` 并 `MarkNotReady`

- [x] Task 5: 验证
  - [x] SubTask 5.1: `dotnet build` 全绿（主程序 + samples/OpenAiCompatibleEngine）
  - [x] SubTask 5.2: `dotnet test` 全绿（含新增 DeepSeek 测试）
  - [ ] SubTask 5.3: 启动 API，`GET /health/engines` 含 `deepseek-remote`
  - [ ] SubTask 5.4: `/lab/bazi/interpret` 触发降级链命中 deepseek-remote（mock onnx 不可用时），返回真实 DeepSeek 解读

# Task Dependencies
- 依赖 [engine-plugins-three-modes](../engine-plugins-three-modes/spec.md) 已完成（提供 `OpenAiCompatibleEngineBase` 与降级链 Orchestrator）
- Task 1 → Task 2 → Task 3 → Task 4 → Task 5
