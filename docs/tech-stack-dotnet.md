# 技术路线：.NET Lab

## 定位

正式方向是 .NET 10 + ASP.NET Core，代码位于 `src/IChing.Lab.*`。历史技术验证代码已从仓库移除。

## 结构

```text
src/
├── IChing.Lab.Core/        # 八字、六爻、塔罗、黄历、合盘、Layer1 规则
├── IChing.Lab.Inference/   # ONNX GenAI 解读
├── IChing.Lab.Api/         # HTTP API + Blazor Lab
├── IChing.Lab.PromptTest/  # Prompt/模型本地试跑
└── IChing.Lab.Tests/       # 测试
```

SDK 锁定见 `global.json`。

## 依赖

| 模块 | 实现 |
| --- | --- |
| 八字 | `lunar-csharp` + 真太阳时 + 大运流年 + 格局/用神 |
| 六爻 | `IChingLibrary.SixLines`，负责铜钱/时间起卦、纳甲、世应、六亲、六神、伏神、神煞 |
| 塔罗 | 内置 78 张牌、牌阵、正逆位和 Layer1 摘要 |
| AI 解读 | `Microsoft.ML.OnnxRuntimeGenAI` + Qwen2.5-1.5B |

## 规则引擎

`IChing.Lab.Core/Rules` 提供最小插件模型：

- 内置插件注册，不加载外部 DLL。
- `RuleEngine:Plugins:{pluginId}:Enabled` 控制启停。
- `RuleEngine:Plugins:{pluginId}:Weight` 和 `RuleEngine:MinWeight` 控制过滤。
- `ruleDigest.activePlugins` 和 `ruleDigest.items` 返回实际参与的规则。

## 本地运行

```bash
cd src/IChing.Lab.Api
dotnet run
```
