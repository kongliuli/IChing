# Tasks

- [x] Task 1: 创建 IChing.Lab.Abstractions 项目骨架
  - [x] SubTask 1.1: 在 src/ 下新建 IChing.Lab.Abstractions.csproj（net10.0 类库）
  - [x] SubTask 1.2: 加入 src/IChing.Lab.sln
  - [x] SubTask 1.3: 验证 `dotnet build` 通过

- [x] Task 2: 定义共享 DTO 与枚举
  - [x] SubTask 2.1: 定义 `ChartRequest` record（Domain + Args 字典）
  - [x] SubTask 2.2: 定义 `PromptContext` / `PromptBuildResult` / `GenerateOptions` records
  - [x] SubTask 2.3: 定义 `GenerationResult` record（Engine / Text / IsFallback / FallbackReason / ElapsedMs）
  - [x] SubTask 2.4: 定义 `EngineMode` 枚举（InProcess / LocalHttp / RemoteApi）

- [x] Task 3: 定义四个核心接口
  - [x] SubTask 3.1: `IChartEngine`（Domain / EngineId / Calculate）
  - [x] SubTask 3.2: `IPromptBuilder`（Domain / Tier / TemplateId / Build）
  - [x] SubTask 3.3: `IInferenceEngine : IDisposable`（EngineId / IsReady / GenerateAsync）
  - [x] SubTask 3.4: `IPluginModule`（Register(IServiceCollection)）

- [x] Task 4: 定义 manifest 与版本校验
  - [x] SubTask 4.1: `IPluginManifest`（Name / Version / RequiredApiVersion）
  - [x] SubTask 4.2: `AbstractionsVersion` 常量（当前 "0.1"）

- [x] Task 5: 验证编译与基础单测
  - [x] SubTask 5.1: 编写一个内存 mock 实现 `IInferenceEngine`，验证接口可被实现
  - [x] SubTask 5.2: `dotnet build src/IChing.Lab.sln` 全绿

# Task Dependencies
- Task 2 → Task 3（接口用到 DTO）
- Task 3 → Task 4（manifest 引用接口）
- Task 5 依赖前 4 个全部完成
