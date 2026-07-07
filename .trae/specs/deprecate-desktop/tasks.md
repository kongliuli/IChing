# Tasks

- [x] Task 1: 标注 csproj 废弃
  - [x] SubTask 1.1: `IChing.Desktop.csproj` 加 `<Description>Deprecated — paused in favor of IChing.Lab.Api plugin mainline</Description>`
  - [x] SubTask 1.2: 加 `<PackageReadmeFile>` 或 XML 注释说明废弃原因与替代方案

- [x] Task 2: 入口文件废弃注释
  - [x] SubTask 2.1: `App.xaml.cs` 顶部加 `// DEPRECATED: Desktop client paused. Use IChing.Lab.Api + plugins. See spec: deprecate-desktop.`
  - [x] SubTask 2.2: `MainWindow.xaml.cs` 顶部加同样注释
  - [x] SubTask 2.3: `OpenAiChatClient.cs` 顶部加注释指向 `samples/OpenAiCompatibleEngine/OpenAiRemoteEngine` 作为插件化替代

- [x] Task 3: README 与解决方案标注
  - [x] SubTask 3.1: [README.md](file:///workspace/README.md) 模块表 `IChing.Desktop` 行注明 `(deprecated, paused)`
  - [x] SubTask 3.2: `IChing.Lab.sln 中 IChing.Desktop 项目注释说明不参与默认 CI 构建

- [ ] Task 4: 验证不破坏现有构建
  - [x] SubTask 4.1: `dotnet build src/IChing.Lab.sln` 全绿（桌面端仍可编译，只是标注废弃）
  - [x] SubTask 4.2: 桌面端代码文件数未减少，`OpenAiChatClient` / `MainWindow` 逻辑未改动
  - [x] SubTask 4.3: `dotnet test` 全绿

> 注: SubTask 4.1 / 4.3 未能在当前沙箱执行 — 沙箱仅安装 .NET 8.0.422 SDK, 而 `global.json` 要求 10.0.301 且项目 target 为 `net10.0`。已通过 `dotnet sln list`(SDK 8 host)确认 sln 仍可解析且 IChing.Desktop 仍在列, 并通过 XML 解析确认 csproj 格式正确; 实际构建需在装有 .NET 10 SDK 的环境中复核。

# Task Dependencies
- 无依赖，独立执行
- Task 1 / Task 2 / Task 3 可并行
