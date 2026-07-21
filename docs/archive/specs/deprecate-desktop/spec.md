# 桌面端标记废弃 Spec

## Why

`IChing.Desktop`（WPF + OpenAiChatClient）当前与插件化主线脱节：其 `OpenAiChatClient` 与 `samples/OpenAiCompatibleEngine/OpenAiRemoteEngine` 逻辑重复，且桌面端未接入 `IChartEngine` / `IPromptBuilder` / `IInferenceEngine` 插件体系。用户要求：「桌面端标记为废弃，把当前完整内容完善之后再考虑」。本 spec 仅做废弃标记与文档说明，不删除代码，不在桌面端新增功能。

## What Changes

- [IChing.Desktop.csproj](../../../../src/IChing.Desktop/IChing.Desktop.csproj) 新增 `<Description>` 与 XML 注释标注「已废弃，暂停演进」
- `App.xaml.cs` / `MainWindow.xaml.cs` 顶部加 `[Obsolete("Desktop client is deprecated; use IChing.Lab.Api + plugins. Will be revisited after plugin mainline completes.")]` 程序级注释
- [README.md](../../../../README.md) 模块表注明 `IChing.Desktop` 为 deprecated
- 从 `IChing.Lab.sln` 的主构建配置中**保留**项目（不卸载），但标注不参与 CI 默认构建（通过配置项 `DesktopDeprecated=true` 跳过，或文档说明手动构建）
- `OpenAiChatClient.cs` 加注释指向 `samples/OpenAiCompatibleEngine/OpenAiRemoteEngine` 作为替代实现
- **不删除任何桌面端代码**，不回滚用户既有改动

### 约束

- 不在桌面端新增任何功能（包括插件接入、新引擎、新模板）
- 不删除 `IChing.Desktop` 项目与 `OpenAiChatClient`（保留供未来参考与复用）
- 插件化主线完善后，再单开 spec 决定桌面端是改造接入插件还是重写

## Impact

- Affected code: [IChing.Desktop/](../../../../src/IChing.Desktop/)（仅注释/描述）、[README.md](../../../../README.md)、[IChing.Lab.sln](../../../../src/IChing.Lab.sln)（构建配置注释）
- Affected specs: 无依赖，独立执行
- 无破坏性变更：桌面端仍可手动构建运行，只是不再演进

## ADDED Requirements

### Requirement: 桌面端废弃标记

系统 SHALL 在 `IChing.Desktop` 项目与入口文件标注废弃，并文档化替代方案。

#### Scenario: 项目描述可见废弃
- **WHEN** 查看 `IChing.Desktop.csproj`
- **THEN** 含 `<Description>Deprecated — paused in favor of IChing.Lab.Api plugin mainline</Description>`

#### Scenario: 入口文件废弃注释
- **WHEN** 打开 `App.xaml.cs` 或 `MainWindow.xaml.cs`
- **THEN** 顶部含 `// DEPRECATED: Desktop client paused. Use IChing.Lab.Api + plugins. See spec: deprecate-desktop.` 注释

#### Scenario: 替代实现指引
- **WHEN** 查看 `OpenAiChatClient.cs`
- **THEN** 含注释指向 `samples/OpenAiCompatibleEngine/OpenAiRemoteEngine` 作为插件化替代

### Requirement: 不删除不演进

系统 SHALL 保留 `IChing.Desktop` 全部代码不删除，且不在其中新增功能。

#### Scenario: 代码保留
- **WHEN** 执行本 spec 后
- **THEN** `IChing.Desktop/` 目录下文件数不减少，`OpenAiChatClient` / `MainWindow` 逻辑不变
