# IChing

.NET 10 易学实验与本地 App 工作区，覆盖八字、六爻、塔罗、日历、规则引擎、Prompt 与推理编排。

## 入口

| 入口 | 用途 | 命令 |
| --- | --- | --- |
| `IChing.App` | 八字 + 六爻 MAUI App | `scripts/run-iching-app.cmd` |
| `IChing.Tarot.App` | 塔罗 MAUI App | `scripts/run-tarot-app.cmd` |
| `IChing.Lab.Api` | API、Demo、规则插件管理 | `scripts/run-lab-api.cmd` |
| `IChing.Lab.Tests` | 自动化测试 | `scripts/test-lab.cmd` |

## 文档

- [文档索引](docs/README.md)
- [App 入口](docs/apps.md)
- [Lab API](docs/lab-api.md)
- [架构说明](docs/architecture.md)
- [技术栈](docs/tech-stack-dotnet.md)
- [规则/插件设计](docs/plugin-design.md)

## 当前主线

- 正式代码都在 `src/`。
- 官方排盘引擎在 `src/BaziEngines`、`src/LiuyaoEngines`、`src/TarotEngines`、`src/CalendarEngines`。
- `samples/` 只保留插件、sidecar、外部推理示例。
- Java Spike 已移除。
- `IChing.Desktop` 是暂停的旧 WPF shell，默认不参与构建。

## 快速验证

```bash
dotnet test src/IChing.Lab.Tests/IChing.Lab.Tests.csproj --no-restore
```
