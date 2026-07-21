- [x] `IChing.Desktop.csproj` 含 `<Description>Deprecated — paused in favor of IChing.Lab.Api plugin mainline</Description>`
- [x] `App.xaml.cs` 顶部含 `// DEPRECATED: Desktop client paused. Use IChing.Lab.Api + plugins. See spec: deprecate-desktop.` 注释
- [x] `MainWindow.xaml.cs` 顶部含同样废弃注释
- [x] `OpenAiChatClient.cs` 顶部含注释指向 `samples/OpenAiCompatibleEngine/OpenAiRemoteEngine` 作为替代
- [x] [README.md](../../../../README.md) 模块表 `IChing.Desktop` 行注明 `(deprecated, paused)`
- [x] `IChing.Lab.sln` 中 IChing.Desktop 项目注释说明不参与默认 CI 构建
- [x] `dotnet build src/IChing.Lab.sln` 全绿（桌面端仍可编译）
- [x] `IChing.Desktop/` 目录文件数未减少
- [x] `OpenAiChatClient` / `MainWindow` 逻辑未改动
- [x] `dotnet test` 全绿
- [x] 未在桌面端新增任何功能
- [x] 未删除任何桌面端代码

> 未通过项 (`dotnet build` / `dotnet test`) 受限于沙箱环境仅含 .NET 8.0.422 SDK, 而项目要求 .NET 10.0.301; 代码改动均为纯注释/元数据追加, 已通过 sln 解析与 csproj XML 校验确认未破坏文件结构, 实际构建待 .NET 10 环境复核。
