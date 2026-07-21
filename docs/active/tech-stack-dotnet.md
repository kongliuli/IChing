# 技术栈：.NET Lab

## 定位

正式实现路径是 `.NET 10`，代码位于 `src/`。历史 Java/Spring Boot Spike 已移除。

## 核心技术

| 模块 | 技术 |
| --- | --- |
| API | ASP.NET Core Controller + Razor Components |
| App | .NET MAUI |
| Accounts | ASP.NET Core Minimal/Controller + JWT（进程内存储） |
| 八字 | `lunar-csharp` + 真太阳时 + 大运流年 + 格局/用神启发式 |
| 六爻 | `IChingLibrary.SixLines` + 纳甲、世应、六亲、六神、神煞 |
| 塔罗 | 内置 78 张牌、牌阵、正逆位、Deckaura 牌义扩展 |
| 规则 | `IChing.Lab.Core/Rules` 内置插件表 + 权重过滤 |
| 推理 | `Microsoft.ML.OnnxRuntimeGenAI` + 模板 fallback + OpenAI 兼容远程样例 |
| 插件 | `AssemblyLoadContext` + `IChing.Lab.Abstractions` |

运行与验证命令见仓库根 [README.md](../../README.md)。
