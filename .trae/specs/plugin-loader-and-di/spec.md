# Plugin Loader and DI Spec

## Why

前 4 个 spec 已完成抽象与改造，但外部插件 DLL 仍无法动态加载。需要实现 `PluginLoadContext`（基于 [AssemblyLoadContext](https://learn.microsoft.com/dotnet/core/tutorials/creating-app-with-plugin-support)）+ `PluginLoader` + DI 集成，使「拷贝 DLL 到 plugins/ 目录 + 改 appsettings.json」即可启用新插件。

**依赖**：
- [plugin-abstractions](../plugin-abstractions/spec.md)（`IPluginModule` / `IPluginManifest`）
- 前 3 个 spec（引擎/排盘/Prompt 已抽象化）

## What Changes

- 新建 `IChing.Lab.PluginLoader` 项目（或在 Abstractions 内）
- 实现 `PluginLoadContext : AssemblyLoadContext`（`isCollectible: true` + `AssemblyDependencyResolver`）
- 实现 `PluginLoader`：扫描 `plugins/*.dll`，按 manifest 加载，校验 `RequiredApiVersion`
- 实现 `IPluginModule` 注册机制：每个插件 DLL 含一个 `IPluginModule` 实现类，`Register(IServiceCollection)` 自行注册
- `Program.cs` 启动时调用 `PluginLoader.Discover()` + 注册到 DI
- 新建 `plugins/` 目录占位（`.gitkeep`）
- `appsettings.json` 加 `plugins:externalAssemblies` 配置段
- 实现卸载流程：清引用 → `Unload()` → `GC.Collect()` × 2

## Impact

- Affected specs:
  - plugin-abstractions（依赖）
  - 后续 engine-plugins-three-modes（被加载）
- Affected code:
  - 新增：`src/IChing.Lab.PluginLoader/PluginLoadContext.cs` / `PluginLoader.cs` / `PluginManifest.cs`
  - 修改：`src/IChing.Lab.Api/Program.cs`（启动时加载外部插件）
  - 修改：`src/IChing.Lab.Api/appsettings.json`（plugins 段）
  - 新增：`plugins/.gitkeep`

## ADDED Requirements

### Requirement: 外部插件 DLL 动态加载

The system SHALL load external plugin assemblies from `plugins/` directory at startup using isolated `AssemblyLoadContext` per plugin.

#### Scenario: 拷贝 DLL 即启用

- **WHEN** 将 `MyEngine.dll` 拷贝到 `plugins/` 并在 `appsettings.json` 注册
- **THEN** 主程序下次启动时加载该 DLL，其 `IPluginModule` 实现被调用，DI 中出现对应服务

### Requirement: 依赖隔离

The system SHALL resolve plugin dependencies via `AssemblyDependencyResolver`, isolating native DLL conflicts (e.g. ONNX vs LLamaSharp CUDA).

#### Scenario: 两插件依赖不同版本的同 NuGet

- **WHEN** PluginA 依赖 `Newtonsoft.Json 12.0.3`，PluginB 依赖 `13.0.1`
- **THEN** 两个插件各自从自己的 `.deps.json` 解析，互不影响

### Requirement: 版本兼容校验

The system SHALL reject plugins whose `IPluginManifest.RequiredApiVersion` is incompatible with `AbstractionsVersion`.

#### Scenario: 接口版本不匹配

- **WHEN** 插件 manifest 声明 `RequiredApiVersion = "0.1"` 但主程序为 `"1.0"`
- **THEN** 该插件被跳过，日志记录 warning，主程序继续启动

### Requirement: 卸载支持

The system SHALL support unloading a plugin via `AssemblyLoadContext.Unload()` followed by GC.

#### Scenario: 卸载后内存释放

- **WHEN** 调用 `PluginLoader.Unload(pluginId)` 并触发 GC
- **THEN** `WeakReference` 到该 ALC 变为 `null`，DLL 文件可被删除

## MODIFIED Requirements

### Requirement: 启动流程包含插件发现

The `Program.cs` SHALL invoke `PluginLoader.Discover()` before `builder.Build()`, registering all `IPluginModule` implementations to DI.

#### Scenario: 无 plugins/ 目录

- **WHEN** `plugins/` 目录不存在
- **THEN** 主程序正常启动，无异常，仅日志 info "no external plugins"
