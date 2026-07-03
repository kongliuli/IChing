# Tasks

- [ ] Task 1: 创建 PluginLoader 项目
  - [ ] SubTask 1.1: 新建 `src/IChing.Lab.PluginLoader/IChing.Lab.PluginLoader.csproj`（net10.0）
  - [ ] SubTask 1.2: 引用 `IChing.Lab.Abstractions` + `Microsoft.Extensions.DependencyInjection.Abstractions`
  - [ ] SubTask 1.3: 加入 `IChing.Lab.sln`

- [ ] Task 2: 实现 PluginLoadContext
  - [ ] SubTask 2.1: 新建 `PluginLoadContext.cs`，继承 `AssemblyLoadContext`，`isCollectible: true`
  - [ ] SubTask 2.2: 注入 `AssemblyDependencyResolver`
  - [ ] SubTask 2.3: 重写 `Load(AssemblyName)` 与 `LoadUnmanagedDll(string)`

- [ ] Task 3: 实现 PluginLoader
  - [ ] SubTask 3.1: 新建 `PluginLoader.cs`，注入 `IConfiguration`（读 `plugins:externalAssemblies`）
  - [ ] SubTask 3.2: `Discover()` 扫描 `plugins/*.dll`，返回 manifest 列表
  - [ ] SubTask 3.3: `LoadAssembly(manifest)` 用独立 `PluginLoadContext` 加载
  - [ ] SubTask 3.4: 校验 `IPluginManifest.RequiredApiVersion` 与 `AbstractionsVersion` 兼容
  - [ ] SubTask 3.5: 扫描 `IPluginModule` 实现类，调用 `Register(IServiceCollection)`

- [ ] Task 4: 实现 PluginManifest 与版本匹配
  - [ ] SubTask 4.1: `PluginManifest` record（Name / Path / RequiredApiVersion）
  - [ ] SubTask 4.2: SemVer 兼容判断（major 相同视为兼容）

- [ ] Task 5: 卸载机制
  - [ ] SubTask 5.1: `PluginLoader.Unload(pluginId)` 调 `context.Unload()`
  - [ ] SubTask 5.2: 触发 `GC.Collect()` + `GC.WaitForPendingFinalizers()` + `GC.Collect()`
  - [ ] SubTask 5.3: `WeakReference` 跟踪 ALC，验证 `IsAlive` 变 false

- [ ] Task 6: Program.cs 集成
  - [ ] SubTask 6.1: `Program.cs` 在 `builder.Build()` 前调用 `PluginLoader.Discover()` + 注册
  - [ ] SubTask 6.2: `appsettings.json` 加 `plugins:externalAssemblies` 数组
  - [ ] SubTask 6.3: 新建 `plugins/.gitkeep` 占位目录
  - [ ] SubTask 6.4: 处理 `plugins/` 目录不存在的降级路径

- [ ] Task 7: 测试与验证
  - [ ] SubTask 7.1: 编写一个 mock 外部插件项目 `samples/SamplePlugin/`，实现 `IPluginModule`
  - [ ] SubTask 7.2: 编译后拷贝到 `plugins/`，启动主程序，验证服务已注册
  - [ ] SubTask 7.3: 测试版本不匹配：mock 声明 `RequiredApiVersion="99.0"`，验证被跳过 + 日志 warning
  - [ ] SubTask 7.4: 测试卸载：调 `Unload` + GC，验证 `WeakReference.IsAlive=false`
  - [ ] SubTask 7.5: `dotnet test` 全绿

# Task Dependencies
- 依赖 [plugin-abstractions](../plugin-abstractions/spec.md) 完成
- 依赖 [refactor-inference-engine](../refactor-inference-engine/spec.md) / [wrap-chart-engines](../wrap-chart-engines/spec.md) / [externalize-prompt-templates](../externalize-prompt-templates/spec.md) 完成（验证集成效果）
- Task 1 → Task 2 → Task 3 → Task 4（并行 Task 5）→ Task 6 → Task 7
