# IChing

命理平台调研 Lab，正式技术栈为 .NET 10。仓库当前只保留 .NET Lab；历史技术验证代码已移除。

## 快速开始

```bash
cd src/IChing.Lab.Api
dotnet run
```

可选：下载本地 ONNX 解读模型。

```bash
bash scripts/download-qwen-15b-model.sh ./models/qwen2.5-1.5b-genai
```

无模型时也可以只跑 prompt dry-run：

```bash
cd src
dotnet run --project IChing.Lab.PromptTest -- --dry-run
```

## Lab API

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/lab/bazi` | 八字排盘 |
| POST | `/lab/bazi/read?tier=0..2` | 八字排盘 + 规则摘要 + 可选模型解读 |
| POST | `/lab/bazi/interpret` | 八字排盘 + ONNX 解读 |
| POST | `/lab/bazi/hepan` | 双人合盘 |
| GET | `/lab/bazi/cities` | 城市经度表 |
| POST | `/lab/liuyao/coin` | 六爻铜钱起卦 + 纳甲 |
| POST | `/lab/liuyao/time` | 六爻时间起卦 + 纳甲 |
| POST | `/lab/liuyao/read?tier=0..2` | 六爻起卦 + 插件化规则摘要 + 可选模型解读 |
| POST | `/lab/tarot/draw` | 塔罗抽牌 |
| POST | `/lab/tarot/read?tier=0..2` | 塔罗抽牌 + 插件化规则摘要 + 可选模型解读 |
| GET | `/lab/tarot/spreads` | 牌阵列表 |
| GET | `/lab/calendar/day` | 黄历日课 |
| POST | `/lab/interpret` | 任意命盘 JSON 短解读 |
| GET | `/lab/interpret/status` | ONNX 模型加载状态 |
| GET | `/lab/rules/plugins` | 规则插件列表 |
| PUT | `/lab/rules/plugins/{id}` | 运行时启停插件或调整权重 |

## 模块

| 项目 | 说明 |
| --- | --- |
| `IChing.Lab.Core` | 八字、六爻、塔罗、黄历、合盘算法和 Layer1 规则引擎 |
| `IChing.Lab.Inference` | ONNX GenAI 解读 |
| `IChing.Lab.Api` | HTTP API 和 Blazor Lab 页面 |
| `IChing.Lab.Tests` | 核心算法和规则引擎测试 |

## 规则引擎

Layer1 规则引擎位于 `src/IChing.Lab.Core/Rules`。v1 使用内置插件表，通过 `appsettings.json` 的 `RuleEngine` 节点设置默认启停和权重。

运行时管理页：

```text
http://localhost:5xxx/rule-plugins
```

## 测试

```bash
dotnet test src/IChing.Lab.sln
```
